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
using Core.Domain.Accounts;
using Core.Domain.Assets;
using Core.Domain.Exchange;
using Core.Domain.Feed;
using Core.Domain.Settings;
using Core.Feed;
using Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Services;
using Swashbuckle.Swagger.Model;
using Lykke.Domain.Prices.Repositories;
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
            var generalSettings = GeneralSettingsReader.ReadGeneralSettings<Settings>(Configuration["ConnectionString"]);
#endif
            var settings = generalSettings.PublicApi;

            // Ignore case of asset in asset connections
            generalSettings.CandleHistoryAssetConnections = 
                new Dictionary<string, string>(generalSettings.CandleHistoryAssetConnections, StringComparer.OrdinalIgnoreCase);

            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddMemoryCache();

            services.AddSingleton(settings);
            services.AddSingleton(settings.CompanyInfo);

            services.AddSingleton<IAssetsRepository>(
                new AssetsRepository(new AzureTableStorage<AssetEntity>(settings.Db.DictsConnString, "Dictionaries",
                    null)));

            services.AddSingleton<IAssetPairsRepository>(
                new AssetPairsRepository(new AzureTableStorage<AssetPairEntity>(settings.Db.DictsConnString, "Dictionaries",
                    null)));

            services.AddSingleton<IAssetPairBestPriceRepository>(
                new AssetPairBestPriceRepository(new AzureTableStorage<FeedDataEntity>(settings.Db.HLiquidityConnString,
                    "MarketProfile", null)));

            services.AddSingleton<IMarketDataRepository>(
                new MarketDataRepository(new AzureTableStorage<MarketDataEntity>(settings.Db.HTradesConnString,
                    "MarketsData", null)));

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
                new FeedHistoryRepository(new AzureTableStorage<FeedHistoryEntity>(settings.Db.HLiquidityConnString,
                    "FeedHistory", null)));

            services.AddSingleton<IWalletsRepository>(
                new WalletsRepository(new AzureTableStorage<WalletEntity>(settings.Db.BalancesInfoConnString,
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
                options.Configuration = settings.CacheSettings.RedisConfiguration;
                options.InstanceName = settings.CacheSettings.FinanceDataCacheInstance;
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

            app.UseMvc();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUi();
        }
    }
}
