using AzureRepositories;
using AzureRepositories.Assets;
using AzureRepositories.Feed;
using AzureRepositories.Reports;
using AzureStorage.Tables;
using Common;
using Core.Domain.Assets;
using Core.Domain.Feed;
using Core.Domain.Reports;
using Core.Domain.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.Swagger.Model;

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
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var settings = GeneralSettingsReader.ReadGeneralSettings<BaseSettings>(Configuration["ConnectionString"]);

            services.AddSingleton<IAssetsRepository>(
                new AssetsRepository(new AzureTableStorage<AssetEntity>(settings.Db.DictsConnString, "Dictionaries",
                    null)));

            services.AddSingleton<IAssetPairsRepository>(
                new AssetPairsRepository(new AzureTableStorage<AssetPairEntity>(settings.Db.DictsConnString, "Dictionaries",
                    null)));

            services.AddSingleton<IAssetPairBestPriceRepository>(
                new AssetPairBestPriceRepository(new AzureTableStorage<FeedDataEntity>(settings.Db.HLiquidityConnString,
                    "MarketProfile", null)));

            var tableVersion = 
                new JobSyncLogRepository(new AzureTableStorage<JobSyncLogRecord>(settings.Db.OlapLogsConnString,
                    "JobSyncLogs", null)).GetTradeReportsTableVersion().Result;

            services.AddSingleton<ITradeVolumesRepository>(
                new TradeVolumesRepository(new AzureTableStorage<TradeVolumesRecord>(settings.Db.OlapConnString,
                    $"AssetSummariesByDays{tableVersion}", null)));

            services.AddMvc();

            services.AddSwaggerGen();

            services.ConfigureSwaggerGen(options =>
            {
                options.SingleApiVersion(new Info
                {
                    Version = "v1",
                    Title = "Lykke public API",
                    TermsOfService = "https://lykke.com/city/terms_of_use",
                    License = new License { Name = "Use under MIT", Url = "https://github.com/LykkeCity/LykkePublicAPI/blob/master/LICENSE" }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
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
