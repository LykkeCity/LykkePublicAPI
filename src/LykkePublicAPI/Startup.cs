using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AzureRepositories;
using AzureRepositories.Accounts;
using AzureRepositories.Assets;
using AzureRepositories.Candles;
using AzureRepositories.Exchange;
using AzureRepositories.Feed;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Core.Domain.Accounts;
using Core.Domain.Assets;
using Core.Domain.Exchange;
using Core.Domain.Feed;
using Core.Domain.Settings;
using Core.Feed;
using Core.Services;
using Lykke.AzureQueueIntegration;
using Lykke.Common.ApiLibrary.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Services;
using Swashbuckle.Swagger.Model;
using Lykke.Domain.Prices.Repositories;
using Lykke.Logs;
using Lykke.SlackNotification.AzureQueue;
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
#if DEBUG
            var generalSettings = GeneralSettingsReader.ReadGeneralSettingsLocal<Settings>(Configuration["ConnectionString"]);
#else
            var generalSettings = HttpSettingsLoader.Load<Settings>(Configuration["ConnectionString"]);
#endif
            var settings = generalSettings;

            // Ignore case of asset in asset connections
            generalSettings.CandleHistoryAssetConnections = 
                new Dictionary<string, string>(generalSettings.CandleHistoryAssetConnections, StringComparer.OrdinalIgnoreCase);

            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddMemoryCache();

            services.AddSingleton(settings);
            services.AddSingleton(settings.PublicApi);
            services.AddSingleton(settings.PublicApi.CompanyInfo);

            var log = CreateLogWithSlack(services, settings);
            services.AddSingleton(log);

            services.AddSingleton<IAssetsRepository>(
                new AssetsRepository(new AzureTableStorage<AssetEntity>(settings.PublicApi.Db.DictsConnString, "Dictionaries",
                    null)));

            services.AddSingleton<IAssetPairsRepository>(
                new AssetPairsRepository(new AzureTableStorage<AssetPairEntity>(settings.PublicApi.Db.DictsConnString, "Dictionaries",
                    null)));

            services.AddSingleton<IAssetPairBestPriceRepository>(
                new AssetPairBestPriceRepository(new AzureTableStorage<FeedDataEntity>(settings.PublicApi.Db.HLiquidityConnString,
                    "MarketProfile", null)));

            services.AddSingleton<IMarketDataRepository>(
                new MarketDataRepository(new AzureTableStorage<MarketDataEntity>(settings.PublicApi.Db.HTradesConnString,
                    "MarketsData", null)));

            services.AddSingleton<ITradesCommonRepository>(
                new TradesCommonRepository(new AzureTableStorage<TradeCommonEntity>(settings.PublicApi.Db.HTradesConnString,
                    "TradesCommon", null)));

            services.AddSingleton<ICandleHistoryRepository>(serviceProvider => new CandleHistoryRepositoryResolver((asset, tableName) =>
            {
                string connString;
                if (!generalSettings.CandleHistoryAssetConnections.TryGetValue(asset, out connString)
                    || string.IsNullOrEmpty(connString))
                {
                    throw new AppSettingException(string.Format("Connection string for asset pair '{0}' is not specified.", asset));
                }

                return new AzureTableStorage<CandleTableEntity>(connString, tableName, null);
            }));

            services.AddSingleton<IFeedHistoryRepository>(
                new FeedHistoryRepository(new AzureTableStorage<FeedHistoryEntity>(settings.PublicApi.Db.HLiquidityConnString,
                    "FeedHistory", null)));

            services.AddSingleton<IWalletsRepository>(
                new WalletsRepository(new AzureTableStorage<WalletEntity>(settings.PublicApi.Db.BalancesInfoConnString,
                    "Accounts", null)));

            services.AddSingleton(x =>
            {
                var assetPairsRepository = (IAssetPairsRepository)x.GetService(typeof(IAssetPairsRepository));
                return new CachedDataDictionary<string, IAssetPair>(
                    async () => (await assetPairsRepository.GetAllAsync()).ToDictionary(itm => itm.Id));
            });

            services.AddSingleton(x =>
            {
                var assetRepository = (IAssetsRepository) x.GetService(typeof(IAssetsRepository));
                return new CachedDataDictionary<string, IAsset>(
                    async () => (await assetRepository.GetAssetsAsync()).ToDictionary(itm => itm.Id));
            });

            services.AddSingleton(x =>
            {
                var assetsRepo = (IAssetsRepository)x.GetService(typeof(IAssetsRepository));
                return new CachedDataDictionary<string, IAsset>(
                    async () => (await assetsRepo.GetAssetsAsync()).ToDictionary(itm => itm.Id));
            });

            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = settings.PublicApi.CacheSettings.RedisConfiguration;
                options.InstanceName = settings.PublicApi.CacheSettings.FinanceDataCacheInstance;
            });

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
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("");
                }

                await next.Invoke();
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
        }

        private static ILog CreateLogWithSlack(IServiceCollection services, Settings settings)
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

            var dbLogConnectionString = settings.PublicApi.Db.LogsConnString;

            // Creating azure storage logger, which logs own messages to concole log
            if (!string.IsNullOrEmpty(dbLogConnectionString) && !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                const string appName = "PublicAPI";

                var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                    appName,
                    AzureTableStorage<LogEntity>.Create(() => dbLogConnectionString, "PublicAPILog", consoleLogger),
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
    }
}
