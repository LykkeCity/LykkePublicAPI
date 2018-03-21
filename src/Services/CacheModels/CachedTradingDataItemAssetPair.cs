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
        public T Parameter { get; set; }
        
        public CachedTradingDataItemAssetPair(AssetPairTradingDataItem<T> source)
        {
            Parameter = source.Parameter;
        }

        [UsedImplicitly]
        public CachedTradingDataItemAssetPair()
        {
        }

        public AssetPairTradingDataItem<T> ToModel(string assetPair)
        {
            return new AssetPairTradingDataItem<T>(assetPair, Parameter);
        }
    }
}
