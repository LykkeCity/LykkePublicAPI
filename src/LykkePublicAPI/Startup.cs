using System;
using System.IO;
using System.Threading.Tasks;
using AspNetCoreRateLimit;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Core.Domain.Settings;
using Lykke.Common.ApiLibrary.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.Swagger.Model;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using LykkePublicAPI.Modules;
using Microsoft.AspNetCore.Http;

namespace LykkePublicAPI
{
    public class Startup
    {
        private IConfigurationRoot Configuration { get; }

        private ILog Log { get; set; }

        private IContainer ApplicationContainer { get; set; }

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
        
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var builder = new ContainerBuilder();

            var appSettings = Configuration.LoadSettings<Settings>();
            var settings = appSettings.CurrentValue;
            var dbSettings = appSettings.Nested(x => x.PublicApi.Db);

            Log = CreateLogWithSlack(services, settings, dbSettings);

            services.AddApplicationInsightsTelemetry(Configuration);
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

            builder.RegisterModule(new ApiModule(settings, Log));
            builder.RegisterModule(new RepositoriesModule(dbSettings));

            builder.Populate(services);

            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
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

            appLifetime.ApplicationStarted.Register(() => StartApplication().GetAwaiter().GetResult());
            appLifetime.ApplicationStopping.Register(() => StopApplication().GetAwaiter().GetResult());
            appLifetime.ApplicationStopped.Register(() => CleanUp().GetAwaiter().GetResult());
        }

        private async Task StartApplication()
        {
            try
            {
                // NOTE: Job not yet recieve and process IsAlive requests here
                await Log.WriteMonitorAsync("", "", "Started");
            }
            catch (Exception ex)
            {
                await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StartApplication), "", ex);
                throw;
            }
        }

        private async Task StopApplication()
        {
            try
            {
                // NOTE: Job still can recieve and process IsAlive requests here, so take care about it if you add logic here.
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StopApplication), "", ex);
                }
                throw;
            }
        }

        private async Task CleanUp()
        {
            try
            {
                // NOTE: Job can't recieve and process IsAlive requests here, so you can destroy all resources

                if (Log != null)
                {
                    await Log.WriteMonitorAsync("", "", "Terminating");
                }

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(CleanUp), "", ex);
                    (Log as IDisposable)?.Dispose();
                }
                throw;
            }
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
    }
}
