using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Antares.Service.MarketProfile.Client;
using AspNetCoreRateLimit;
using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Core;
using Core.Domain.Market;
using Core.Domain.Settings;
using Core.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.Client;
using Lykke.Service.Registration;
using Lykke.Service.TradesAdapter.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
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

            builder.RegisterInstance(_settings);
            builder.RegisterInstance(_apiSettings);
            builder.RegisterInstance(_apiSettings.CompanyInfo);

            builder.RegisterType<OrderBookService>()
                .As<IOrderBooksService>()
                .WithParameter(ResolvedParameter.ForKeyed<IDistributedCache>(CacheType.FinanceData));
            builder.RegisterType<SrvRateHelper>().As<ISrvRatesHelper>();

            // Services that should be decorated

            builder.RegisterType<MarketCapitalizationService>().Named<IMarketCapitalizationService>("default");
            builder.RegisterType<MarketProfileService>().Named<IMarketProfileService>("default");
            builder.RegisterType<MarketTradingDataService>()
                .Named<IMarketTradingDataService>("default")
                .WithParameter("isMtEnabled", !_apiSettings.IsMtDisabled ?? true);

            // Cached decorators

            builder.RegisterDecorator<IMarketCapitalizationService>(
                (c, inner) => new CachedMarketCapitalizationService(
                    c.Resolve<IDistributedCache>(),
                    inner,
                    _settings.PublicApi.CacheSettings.MarketCapitalizationExpirationPeriod),
                fromKey: "default");

            builder.RegisterDecorator<IMarketProfileService>(
                (c, inner) => new CachedMarketProfileService(
                    c.Resolve<IDistributedCache>(),
                    inner,
                    _settings.PublicApi.CacheSettings.MarketProfileExpirationPeriod),
                fromKey: "default");

            builder.RegisterDecorator<IMarketTradingDataService>(
                (c, inner) => new CachedMarketTradingDataService(
                    c.Resolve<IDistributedCache>(),
                    inner,
                    _settings.PublicApi.CacheSettings.MarketTradingDataVolumeExpirationPeriod,
                    _settings.PublicApi.CacheSettings.MarketTradingDataLastTradePriceExpirationPeriod),
                fromKey: "default");

            builder.RegisterType<RegistrationsInfoCacheService>()
                .As<IRegistrationsInfoCacheService>()
                .WithParameter(TypedParameter.From(_settings.PublicApi.CacheSettings.RegistrationsInfoExpirationPeriod))
                .SingleInstance();

            builder.RegisterType<NinjaNetworkClient>()
                .As<INinjaNetworkClient>()
                .WithParameter(TypedParameter.From(_settings.NinjaServiceClient.ServiceUrl));

            RegisterServiceClients(builder);
            RegisterRedisCache(builder);
            ConfigureRateLimits();

            builder.Populate(_services);
        }

        private void RegisterServiceClients(ContainerBuilder builder)
        {
            _services.RegisterAssetsClient(AssetServiceSettings.Create(new Uri(_settings.Assets.ServiceUrl),
                _apiSettings.AssetsCache.ExpirationPeriod), _log);
            
            builder.RegisterTradesAdapterClient(_settings.TradesAdapterServiceClient, _log);

            builder.Register(c =>
                {
                    var provider = new CandlesHistoryServiceProvider();

                    provider.RegisterMarket(MarketType.Spot, _settings.CandlesHistoryServiceClient.ServiceUrl);
                    if (!_settings.PublicApi.IsMtDisabled.HasValue || !_settings.PublicApi.IsMtDisabled.Value)
                        provider.RegisterMarket(MarketType.Mt, _settings.MtCandlesHistoryServiceClient.ServiceUrl);

                    return provider;
                })
                .As<ICandlesHistoryServiceProvider>()
                .SingleInstance();

            builder.Register((x) =>
            {
                var marketProfile = new MarketProfileServiceClient(_settings.PublicApi.MyNoSqlServer.ReaderServiceUrl, _settings.MarketProfileServiceClient.ServiceUrl);
                marketProfile.Start();

                return marketProfile;
            })
            .As<IMarketProfileServiceClient>()
            .SingleInstance()
            .AutoActivate();
            
            builder.RegisterRegistrationClient(_settings.RegistrationServiceClient.ServiceUrl, _log);
            builder.RegisterBalancesClient(_settings.BalancesServiceClient.ServiceUrl, _log);
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
