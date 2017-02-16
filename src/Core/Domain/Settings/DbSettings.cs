namespace Core.Domain.Settings
{
    public class DbSettings
    {
        public string ClientPersonalInfoConnString { get; set; }
        public string BalancesInfoConnString { get; set; }
        public string ALimitOrdersConnString { get; set; }
        public string HLimitOrdersConnString { get; set; }
        public string HMarketOrdersConnString { get; set; }
        public string HTradesConnString { get; set; }
        public string HLiquidityConnString { get; set; }
        public string BackOfficeConnString { get; set; }
        public string BitCoinQueueConnectionString { get; set; }
        public string DictsConnString { get; set; }
        public string LogsConnString { get; set; }
        public string SharedStorageConnString { get; set; }
        public string OlapConnString { get; set; }
        public string OlapLogsConnString { get; set; }
    }

    public class CacheSettings
    {
        public string FinanceDataCacheInstance { get; set; }
        public string RedisConfiguration { get; set; }

        public string OrderBooksCacheKeyPattern { get; set; }
    }

    public static class CacheSettingsExt
    {
        public static string GetOrderBookKey(this CacheSettings settings, string assetPairId, bool isBuy)
        {
            return string.Format(settings.OrderBooksCacheKeyPattern, assetPairId, isBuy);
        }
    }

    public class PublicApiSettings
    {
        public string[] CrossdomainOrigins { get; set; }
    }

    public class BaseSettings
    {
        public DbSettings Db { get; set; }
        public CacheSettings CacheSettings { get; set; }
        public PublicApiSettings PublicApiSettings { get; set; }
    }
}
