using System;
using Antares.Service.MarketProfile.LykkeClient.Models;
using JetBrains.Annotations;
using MessagePack;

namespace Services.CacheModels
{
    [MessagePackObject]
    public class CachedMarketProfileAssetPair
    {
        [Key(0)]
        [UsedImplicitly]
        public double Ask { get; set; }

        [Key(1)]
        [UsedImplicitly]
        public double Bid { get; set; }

        [Key(2)]
        [UsedImplicitly]
        public DateTime AskTimestamp { get; set; }

        [Key(3)]
        [UsedImplicitly]
        public DateTime BidTimestamp { get; set; }

        [UsedImplicitly]
        public CachedMarketProfileAssetPair()
        {
        }

        public CachedMarketProfileAssetPair(AssetPairModel source)
        {
            Ask = source.AskPrice;
            Bid = source.BidPrice;
            AskTimestamp = source.AskPriceTimestamp;
            BidTimestamp = source.BidPriceTimestamp;
        }

        public AssetPairModel ToModel(string assetPair)
        {
            return new AssetPairModel(assetPair, Bid, Ask, BidTimestamp, AskTimestamp);
        }
    }
}
