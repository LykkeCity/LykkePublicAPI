using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AspNetCoreRateLimit;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureRepositories.Accounts;
using AzureRepositories.Exchange;
using AzureRepositories.Feed;
using AzureStorage.Tables;
using Common.Log;
using Core.Domain.Accounts;
using Core.Domain.Exchange;
using Core.Domain.Feed;
using Core.Domain.Market;
using Core.Domain.Settings;
using Core.Feed;
using Core.Services;
using Lykke.Common.ApiLibrary.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Services;
using Swashbuckle.Swagger.Model;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Lykke.MarketProfileService.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Options;

namespace LykkePublicAPI
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var builder = new ContainerBuilder();

            var appSettings = Configuration.LoadSettings<Settings>();
            var settings = appSettings.CurrentValue;
            var dbSettings = appSettings.Nested(x => x.PublicApi.Db);

            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddMemoryCache();

            services.AddSingleton(settings);
            services.AddSingleton(settings.PublicApi);
            services.AddSingleton(settings.PublicApi.CompanyInfo);

            services.UseAssetsClient(AssetServiceSettings.Create(
                new Uri(settings.Assets.ServiceUrl), 
                settings.PublicApi.AssetsCache.ExpirationPeriod));

            var log = CreateLogWithSlack(services, settings, dbSettings);

            services.AddSingleton(log);

            ConfigureRateLimits(
                settings.PublicApi.CacheSettings,
                services,
                settings.PublicApi.IpRateLimiting);

            services.AddSingleton<IAssetPairBestPriceRepository>(
                new AssetPairBestPriceRepository(AzureTableStorage<FeedDataEntity>.Create(
                    dbSettings.ConnectionString(x => x.HLiquidityConnString),
                    "MarketProfile",
                    null)));

            services.AddSingleton<ICandlesHistoryServiceProvider>(x =>
            {
                var provider = new CandlesHistoryServiceProvider();

                provider.RegisterMarket(MarketType.Spot, settings.CandlesHistoryServiceClient.ServiceUrl);
                provider.RegisterMarket(MarketType.Mt, settings.MtCandlesHistoryServiceClient.ServiceUrl);

                return provider;
            });

            // Sets the spot candles history service as default
            services.AddSingleton(x => x.GetService<ICandlesHistoryServiceProvider>().Get(MarketType.Spot));

            services.AddSingleton<ITradesCommonRepository>(
                new TradesCommonRepository(AzureTableStorage<TradeCommonEntity>.Create(
                    dbSettings.ConnectionString(x => x.HTradesConnString),
                    "TradesCommon",
                    null)));
            services.AddSingleton<IFeedHistoryRepository>(
                new FeedHistoryRepository(AzureTableStorage<FeedHistoryEntity>.Create(
                    dbSettings.ConnectionString(x => x.HLiquidityConnString),
                    "FeedHistory",
                    null)));

            services.AddSingleton<IWalletsRepository>(
                new WalletsRepository(AzureTableStorage<WalletEntity>.Create(
                    dbSettings.ConnectionString(x => x.BalancesInfoConnString),
                    "Accounts",
                    null)));

            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = settings.PublicApi.CacheSettings.RedisConfiguration;
                options.InstanceName = settings.PublicApi.CacheSettings.FinanceDataCacheInstance;
            });

            services.AddSingleton<ILykkeMarketProfileServiceAPI>(x => new LykkeMarketProfileServiceAPI(
                new Uri(settings.MarketProfileServiceClient.ServiceUrl)));

            services.AddTransient<IOrderBooksService, OrderBookService>();
            services.AddTransient<IMarketCapitalizationService, MarketCapitalizationService>();
            services.AddTransient<IMarketProfileService, MarketProfileService>();
            services.AddTransient<ISrvRatesHelper, SrvRateHelper>();

            services.AddMvc();

            services.AddSwaggerGen();

            services.ConfigureSwaggerGen(options =>
            {
                options.SingleApiVersion(new Info
                {
                    Version = "v1",
                    Title = "",
                    TermsOfService = "https://lykke.com/city/terms_of_use"
                });

                options.DescribeAllEnumsAsStrings();

                //Determine base path for the application.
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;

                //Set the comments path for the swagger json and ui.
                var xmlPath = Path.Combine(basePath, "LykkePublicAPI.xml");
                options.IncludeXmlComments(xmlPath);
            });

            builder.Populate(services);

            var applicationContainer = builder.Build();

            return new AutofacServiceProvider(applicationContainer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("");
                }
                else
                {
                    await next.Invoke();
                }
            });

            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseLykkeMiddleware("PublicAPI", ex => new { Message = "Technical problem" });
            app.UseClientRateLimiting();

            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUi();
            appLifetime.ApplicationStopped.Register(() => CleanUp(app.ApplicationServices));
        }

        private void CleanUp(IServiceProvider services)
        {
            Console.WriteLine("Cleaning up...");

            (services.GetService<ILog>() as IDisposable)?.Dispose();

            Console.WriteLine("Cleaned up");
        }

        private static ILog CreateLogWithSlack(IServiceCollection services, Settings settings, IReloadingManager<DbSettings> dbSettings)
        {
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();

            aggregateLogger.AddLog(consoleLogger);

            // Creating slack notification service, which logs own azure queue processing messages to aggregate log
            var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new Lykke.AzureQueueIntegration.AzureQueueSettings
            {
                ConnectionString = settings.SlackNotifications.AzureQueue.ConnectionString,
                QueueName = settings.SlackNotifications.AzureQueue.QueueName
            }, aggregateLogger);

            var dbLogConnectionString = dbSettings.CurrentValue.LogsConnString;

            // Creating azure storage logger, which logs own messages to concole log
            if (!string.IsNullOrEmpty(dbLogConnectionString) && !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                const string appName = "PublicAPI";

                var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                    appName,
                    AzureTableStorage<LogEntity>.Create(dbSettings.ConnectionString(x => x.LogsConnString), "PublicAPILog", consoleLogger),
                    consoleLogger);

                var slackNotificationsManager = new LykkeLogToAzureSlackNotificationsManager(appName, slackService, consoleLogger);

                var azureStorageLogger = new LykkeLogToAzureStorage(
                    appName,
                    persistenceManager,
                    slackNotificationsManager,
                    consoleLogger);

                azureStorageLogger.Start();

                aggregateLogger.AddLog(azureStorageLogger);
            }

            return aggregateLogger;
        }

        private void ConfigureRateLimits(CacheSettings settings, IServiceCollection services, RateLimitSettings.RateLimitCoreOptions rateLimitOptions)
        {
            services.Configure<ClientRateLimitOptions>(options =>
            {
                options.EnableEndpointRateLimiting = rateLimitOptions.EnableEndpointRateLimiting;
                options.StackBlockedRequests = rateLimitOptions.StackBlockedRequests;
                options.GeneralRules = rateLimitOptions.GeneralRules
                    .Select(x => new RateLimitRule
                    {
                        Endpoint = x.Endpoint,
                        Limit = x.Limit,
                        PeriodTimespan = x.Period
                    })
                    .ToList();
            });

            var cache = new RedisCache(new RedisCacheOptions
            {
                Configuration = settings.ThrottlingRedisConfiguration,
                InstanceName = settings.ThrottlingInstanceName
            });

            services.AddSingleton<IClientPolicyStore>(c => new DistributedCacheClientPolicyStore(
                cache,
                c.GetService<IOptions<ClientRateLimitOptions>>()));
            services.AddSingleton<IRateLimitCounterStore>(c => new DistributedCacheRateLimitCounterStore(cache));
        }
    }
}
