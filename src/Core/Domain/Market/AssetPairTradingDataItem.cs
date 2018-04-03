namespace Core.Domain.Market
{
    public class AssetPairTradingDataItem<T>
    {
        public string AssetPair { get; }
        public T Value { get; }

        public AssetPairTradingDataItem(string assetPair, T value)
        {
            AssetPair = assetPair;
            Value = value;
        }
    }
}
