namespace Core
{
    public enum NetworkType
    {
        Main,
        Testnet
    }

    public static class LykkeConstants
    {
        public const string BitcoinAssetId = "BTC";
        public const string LykkeAssetId = "LKK";

        public const string UsdAssetId = "USD";
        public const string EurAssetId = "EUR";
        public const string ChfAssetId = "CHF";
        public const string GbpAssetId = "GBP";
        public const string EthAssetId = "ETH";

        public const string LKKUSDPairId = "LKKUSD";

        public const int TotalLykkeAmount = 1250000000;

        public const int MinPwdLength = 6;
        public const int MaxPwdLength = 100;
    }
}