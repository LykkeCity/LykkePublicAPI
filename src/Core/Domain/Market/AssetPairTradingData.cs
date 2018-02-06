namespace Core.Domain.Market
{
    public class AssetPairTradingData
    {
        public string AssetPair { get; }
        public double LastTradePrice { get; }
        public double Volume24 { get; }

        public AssetPairTradingData(string assetPair, double lastTradePrice, double volume24)
        {
            AssetPair = assetPair;
            LastTradePrice = lastTradePrice;
            Volume24 = volume24;
        }
    }
}
