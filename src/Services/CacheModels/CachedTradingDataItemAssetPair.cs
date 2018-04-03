using System;
using System.Collections.Generic;
using System.Text;
using Core.Domain.Market;
using JetBrains.Annotations;
using MessagePack;

namespace Services.CacheModels
{
    [MessagePackObject]
    public class CachedTradingDataItemAssetPair<T>
    {
        [Key(0)]
        [UsedImplicitly]
        public T Value { get; set; }
        
        public CachedTradingDataItemAssetPair(AssetPairTradingDataItem<T> source)
        {
            Value = source.Value;
        }

        [UsedImplicitly]
        public CachedTradingDataItemAssetPair()
        {
        }

        public AssetPairTradingDataItem<T> ToModel(string assetPair)
        {
            return new AssetPairTradingDataItem<T>(assetPair, Value);
        }
    }
}
