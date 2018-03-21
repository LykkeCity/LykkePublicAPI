namespace Core.Domain.Market
{
    public class AssetPairTradingDataItem<T>
    {
        public string AssetPair { get; }
        public T Parameter { get; }

        public AssetPairTradingDataItem(string assetPair, T parameter)
        {
            AssetPair = assetPair;
            Parameter = parameter;
        }
    }
}
