using System;
using System.Linq;
using AspNetCoreRateLimit;
using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Core;
using Core.Domain.Market;
using Core.Domain.Settings;
using Core.Services;
using Lykke.MarketProfileService.Client;
using Lykke.Service.Assets.Client.Custom;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Services;

namespace LykkePublicAPI.Modules
{
    public class ApiModule : Module
    {
        private readonly Settings _settings;
        private readonly PublicApiSettings _apiSettings;
        private readonly ILog _log;
        private readonly ServiceCollection _services;

        public ApiModule(Settings settings, ILog log)
        {
            _settings = settings;
            _apiSettings = settings.PublicApi;
            _log = log;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log);

            _services.AddMemoryCache();

            builder.RegisterInstance(_settings);
            builder.RegisterInstance(_apiSettings);
            builder.RegisterInstance(_apiSettings.CompanyInfo);

            builder.RegisterType<OrderBookService>()
                .As<IOrderBooksService>()
                .WithParameter(ResolvedParameter.ForKeyed<IDistributedCache>(CacheType.FinanceData));

            builder.RegisterType< MarketCapitalizationService>().As<IMarketCapitalizationService>();
            builder.RegisterType<MarketProfileService>().As<IMarketProfileService>();
            builder.RegisterType<SrvRateHelper>().As<ISrvRatesHelper>();

            RegisterServiceClients(builder);
            RegisterRedisCache(builder);
            ConfigureRateLimits();

            builder.Populate(_services);
        }

        private void RegisterServiceClients(ContainerBuilder builder)
        {
            _services.UseAssetsClient(AssetServiceSettings.Create(new Uri(_settings.Assets.ServiceUrl),
                _apiSettings.AssetsCache.ExpirationPeriod));

            builder.Register(c =>
                {
                    var provider = new CandlesHistoryServiceProvider();

                    provider.RegisterMarket(MarketType.Spot, _settings.CandlesHistoryServiceClient.ServiceUrl);
                    provider.RegisterMarket(MarketType.Mt, _settings.MtCandlesHistoryServiceClient.ServiceUrl);

                    return provider;
                })
                .As<ICandlesHistoryServiceProvider>();

            // Sets the spot candles history service as default

            builder.Register(c => c.Resolve<ICandlesHistoryServiceProvider>().Get(MarketType.Spot))
                .SingleInstance();

            builder.RegisterType<LykkeMarketProfileServiceAPI>()
                .As<ILykkeMarketProfileServiceAPI>()
                .WithParameter(TypedParameter.From(new Uri(_settings.MarketProfileServiceClient.ServiceUrl)))
                .SingleInstance();
        }

        private void RegisterRedisCache(ContainerBuilder builder)
        {
            builder
                .Register(c => new RedisCache(new RedisCacheOptions
                {
                    Configuration = _apiSettings.CacheSettings.RedisConfiguration,
                    InstanceName = _apiSettings.CacheSettings.FinanceDataCacheInstance
                }))
                .Keyed<IDistributedCache>(CacheType.FinanceData)
                .SingleInstance();

            builder
                .Register(c => new RedisCache(new RedisCacheOptions
                {
                    Configuration = _apiSettings.CacheSettings.CommonRedisConfiguration,
                    InstanceName = _apiSettings.CacheSettings.CommonRedisInstanceName != null
                        ? $"PublicApi:{_apiSettings.CacheSettings.CommonRedisInstanceName}:"
                        : "PublicApi:"
                }))
                .As<IDistributedCache>()
                .SingleInstance();
        }

        private void ConfigureRateLimits()
        {
            _services.Configure<IpRateLimitOptions>(options =>
            {
                options.EnableEndpointRateLimiting = _apiSettings.IpRateLimiting.EnableEndpointRateLimiting;
                options.StackBlockedRequests = _apiSettings.IpRateLimiting.StackBlockedRequests;
                options.GeneralRules = _apiSettings.IpRateLimiting.GeneralRules
                    .Select(x => new RateLimitRule
                    {
                        Endpoint = x.Endpoint,
                        Limit = x.Limit,
                        Period = x.Period
                    })
                    .ToList();
            });

            _services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            _services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        }
    }
}
