using System;
using System.IO;
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
using Lykke.Service.Registration;
using Microsoft.AspNetCore.Http;

namespace LykkePublicAPI
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var appSettings = Configuration.LoadSettings<Settings>();
            var settings = appSettings.CurrentValue;
            
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddMemoryCache();

            services.AddSingleton(settings);
            services.AddSingleton(settings.PublicApi);
            services.AddSingleton(settings.PublicApi.CompanyInfo);

            services.UseAssetsClient(AssetServiceSettings.Create(
                new Uri(settings.Assets.ServiceUrl), 
                settings.PublicApi.AssetsCache.ExpirationPeriod));

            var log = CreateLogWithSlack(services, appSettings);
            services.AddSingleton(log);

            services.AddSingleton<IAssetPairBestPriceRepository>(
                new AssetPairBestPriceRepository(AzureTableStorage<FeedDataEntity>.Create(appSettings.ConnectionString(x => x.PublicApi.Db.HLiquidityConnString),
                    "MarketProfile", null)));

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
                new TradesCommonRepository(AzureTableStorage<TradeCommonEntity>.Create(appSettings.ConnectionString(x => x.PublicApi.Db.HTradesConnString),
                    "TradesCommon", null)));
            services.AddSingleton<IFeedHistoryRepository>(
                new FeedHistoryRepository(AzureTableStorage<FeedHistoryEntity>.Create(appSettings.ConnectionString(x => x.PublicApi.Db.HLiquidityConnString),
                    "FeedHistory", null)));

            services.AddSingleton<IWalletsRepository>(
                new WalletsRepository(AzureTableStorage<WalletEntity>.Create(appSettings.ConnectionString(x => x.PublicApi.Db.BalancesInfoConnString),
                    "Accounts", null)));

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
            services.AddSingleton<ILykkeRegistrationClient>(x => new LykkeRegistrationClient(settings.RegistrationServiceClient.ServiceUrl, log));

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

        private static ILog CreateLogWithSlack(IServiceCollection services, IReloadingManager<Settings> settings)
        {
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();

            aggregateLogger.AddLog(consoleLogger);

            var dbLogConnectionStringManager = settings.Nested(x => x.PublicApi.Db.LogsConnString);
            var dbLogConnectionString = dbLogConnectionStringManager.CurrentValue;

            if (string.IsNullOrEmpty(dbLogConnectionString))
            {
                consoleLogger.WriteWarningAsync(nameof(Startup), nameof(CreateLogWithSlack), "Table loggger is not inited").Wait();
                return aggregateLogger;
            }

            if (dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}"))
                throw new InvalidOperationException($"LogsConnString {dbLogConnectionString} is not filled in settings");

            var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                AzureTableStorage<LogEntity>.Create(dbLogConnectionStringManager, "PublicAPILog", consoleLogger),
                consoleLogger);

            // Creating slack notification service, which logs own azure queue processing messages to aggregate log
            var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new Lykke.AzureQueueIntegration.AzureQueueSettings
            {
                ConnectionString = settings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                QueueName = settings.CurrentValue.SlackNotifications.AzureQueue.QueueName
            }, aggregateLogger);

            var slackNotificationsManager = new LykkeLogToAzureSlackNotificationsManager(slackService, consoleLogger);

            // Creating azure storage logger, which logs own messages to concole log
            var azureStorageLogger = new LykkeLogToAzureStorage(
                persistenceManager,
                slackNotificationsManager,
                consoleLogger);

            azureStorageLogger.Start();

            aggregateLogger.AddLog(azureStorageLogger);

            return aggregateLogger;
        }
    }
}
