using System;
using System.Collections.Generic;
using Lykke.Service.Balances.Client;
using Lykke.Service.TradesAdapter.Client;
using Lykke.SettingsReader.Attributes;

namespace Core.Domain.Settings
{
    public class Settings
    {
        public PublicApiSettings PublicApi { get; set; }
        public CandlesHistoryServiceClientSettings CandlesHistoryServiceClient { get; set; }
        public CandlesHistoryServiceClientSettings MtCandlesHistoryServiceClient { get; set; }
        public NinjaServiceClientSettings NinjaServiceClient { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public AssetsSettings Assets { get; set; }
        public MarketProfileServiceClientSettings MarketProfileServiceClient { get; set; }
        public RegistrationServiceClientSettings RegistrationServiceClient { get; set; }
        public TradesAdapterServiceClientSettings TradesAdapterServiceClient { get; set; }
        public BalancesServiceClientSettings BalancesServiceClient { get; set; }
    }

    public class RateLimitSettings
    {
        public class RateLimitCoreOptions
        {
            public IReadOnlyList<RateLimitRule> GeneralRules { get; set; }
            public bool StackBlockedRequests { get; set; }
            public bool EnableEndpointRateLimiting { get; set; }
        }
        public class RateLimitRule
        {
            public string Endpoint { get; set; }
            public string Period { get; set; }
            public long Limit { get; set; }
        }
    }

    public class SlackNotificationsSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }

    public class AzureQueueSettings
    {
        [AzureQueueCheck]
        public string ConnectionString { get; set; }

        public string QueueName { get; set; }
    }

    public class PublicApiSettings
    {
        public MyNoSqlSettings MyNoSqlServer { get; set; }
        public DbSettings Db { get; set; }
        public CacheSettings CacheSettings { get; set; }
        public string[] CrossdomainOrigins { get; set; }
        public LykkeCompanyData CompanyInfo { get; set; }
        public AssetsCacheSettings AssetsCache { get; set; }
        [Optional]
        public bool? IsMtDisabled { get; set; }
        public string CandlesToken { get; set; }
        public RateLimitSettings.RateLimitCoreOptions IpRateLimiting { get; set; }
    }

    public class NinjaServiceClientSettings
    {
        public string ServiceUrl { get; set; }
    }

    public class MarketProfileServiceClientSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }

    public class RegistrationServiceClientSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }

    public class AssetsSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }

    public class AssetsCacheSettings
    {
        public TimeSpan ExpirationPeriod { get; set; }
    }

    public class CandlesHistoryServiceClientSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }

    public class LykkeCompanyData
    {
        public double LkkTotalAmount { get; set; }
        public string[] LkkTreasuryWallets { get; set; }
    }

    public class DbSettings
    {
        [AzureTableCheck]
        public string HTradesConnString { get; set; }
        [AzureTableCheck]
        public string HLiquidityConnString { get; set; }
        //[AzureTableCheck]
        public string LogsConnString { get; set; }
    }

    public class CacheSettings
    {
        public string FinanceDataCacheInstance { get; set; }
        public string RedisConfiguration { get; set; }
        public string OrderBooksCacheKeyPattern { get; set; }
        public string CommonRedisConfiguration { get; set; }
        [Optional]
        public string CommonRedisInstanceName { get; set; }

        public TimeSpan MarketCapitalizationExpirationPeriod { get; set; }
        public TimeSpan MarketProfileExpirationPeriod { get; set; }
        public TimeSpan MarketTradingDataVolumeExpirationPeriod { get; set; }
        public TimeSpan MarketTradingDataLastTradePriceExpirationPeriod { get; set; }
        public TimeSpan RegistrationsInfoExpirationPeriod { get; set; }
    }

    public static class CacheSettingsExt
    {
        public static string GetOrderBookKey(this CacheSettings settings, string assetPairId, bool isBuy)
        {
            return string.Format(settings.OrderBooksCacheKeyPattern, assetPairId, isBuy);
        }
    }

    public class MyNoSqlSettings
    {
        public string ReaderServiceUrl { get; set; }
    }
}
