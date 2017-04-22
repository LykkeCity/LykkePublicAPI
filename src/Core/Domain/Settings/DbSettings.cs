namespace Core.Domain.Settings
{
    public class Settings
    {
        public PublicApiSettings PublicApi { get; set; }
    }

    public class PublicApiSettings
    {
        public DbSettings Db { get; set; }
        public CacheSettings CacheSettings { get; set; }
        public string[] CrossdomainOrigins { get; set; }
    }

    public class DbSettings
    {
        public string HTradesConnString { get; set; }
        public string BalancesInfoConnString { get; set; }
        public string HLiquidityConnString { get; set; }
        public string DictsConnString { get; set; }
        public string LogsConnString { get; set; }
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
}
