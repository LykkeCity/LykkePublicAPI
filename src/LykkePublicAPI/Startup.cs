using System;
using System.Buffers;
using System.IO;
using System.Linq;
using AspNetCoreRateLimit;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Core.Domain.Settings;
using Lykke.Common;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using LykkePublicAPI.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.Swagger;

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

            Configuration = builder.Build();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var builder = new ContainerBuilder();

            var appSettings = Configuration.LoadSettings<Settings>(o =>
            {
                o.SetConnString(s => s.SlackNotifications.AzureQueue.ConnectionString);
                o.SetQueueName(s => s.SlackNotifications.AzureQueue.QueueName);
                o.SenderName = $"{AppEnvironment.Name} {AppEnvironment.Version}";
            });
            var settings = appSettings.CurrentValue;
            var dbSettings = appSettings.Nested(x => x.PublicApi.Db);

            Log = CreateLogWithSlack(services, settings, dbSettings);

            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddMvc();
            services.Configure<MvcOptions>(opts =>
            {
                var formatter = opts.OutputFormatters.FirstOrDefault(i => i.GetType() == typeof(JsonOutputFormatter));
                var jsonFormatter = formatter as JsonOutputFormatter;
                var formatterSettings = jsonFormatter == null
                    ? JsonSerializerSettingsProvider.CreateSerializerSettings()
                    : jsonFormatter.PublicSerializerSettings;
                if (formatter != null)
                    opts.OutputFormatters.RemoveType<JsonOutputFormatter>();
                formatterSettings.DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ";
                JsonOutputFormatter jsonOutputFormatter = new JsonOutputFormatter(formatterSettings, ArrayPool<char>.Create());
                opts.OutputFormatters.Insert(0, jsonOutputFormatter);
            });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(
                    "v1",
                    new Info
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

            builder.RegisterModule(new ApiModule(settings, Log));
            builder.RegisterModule(new RepositoriesModule(dbSettings));
            
            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin();
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowCredentials();
            });
            
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
            app.UseIpRateLimiting();

            app.UseMvc();

            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) => swagger.Host = httpReq.Host.Value);
            });
            app.UseSwaggerUI(x =>
            {
                x.RoutePrefix = "swagger/ui";
                x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");;
            });

            appLifetime.ApplicationStarted.Register(StartApplication);
            appLifetime.ApplicationStopping.Register(StopApplication);
            appLifetime.ApplicationStopped.Register(CleanUp);
        }

        private void StartApplication()
        {
            try
            {
                // NOTE: Job not yet recieve and process IsAlive requests here
                Log.WriteMonitor("", "", "Started");
            }
            catch (Exception ex)
            {
                Log.WriteFatalError(nameof(Startup), nameof(StartApplication), ex);
                throw;
            }
        }

        private void StopApplication()
        {
            try
            {
                // NOTE: Job still can recieve and process IsAlive requests here, so take care about it if you add logic here.
            }
            catch (Exception ex)
            {
                Log?.WriteFatalError(nameof(Startup), nameof(StopApplication), ex);
                throw;
            }
        }

        private void CleanUp()
        {
            try
            {
                // NOTE: Job can't recieve and process IsAlive requests here, so you can destroy all resources

                Log?.WriteMonitor("", "", "Terminating");

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    Log.WriteFatalError(nameof(Startup), nameof(CleanUp), ex);
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
