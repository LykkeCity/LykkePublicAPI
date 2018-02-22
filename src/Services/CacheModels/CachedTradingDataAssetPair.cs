using Core.Domain.Market;
using JetBrains.Annotations;
using MessagePack;

namespace Services.CacheModels
{
    [MessagePackObject]
    public class CachedTradingDataAssetPair
    {
        [Key(0)]
        [UsedImplicitly]
        public double LastTradePrice { get; set; }

        [Key(1)]
        [UsedImplicitly]
        public double Volume24 { get; set; }

        public CachedTradingDataAssetPair(AssetPairTradingData source)
        {
            LastTradePrice = source.LastTradePrice;
            Volume24 = source.Volume24;
        }

        [UsedImplicitly]
        public CachedTradingDataAssetPair()
        {
        }

        public AssetPairTradingData ToModel(string assetPair)
        {
            return new AssetPairTradingData(assetPair, LastTradePrice, Volume24);
        }
    }
}
