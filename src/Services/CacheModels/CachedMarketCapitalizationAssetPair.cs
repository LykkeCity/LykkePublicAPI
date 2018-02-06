using JetBrains.Annotations;
using MessagePack;

namespace Services.CacheModels
{
    [MessagePackObject]
    public class CachedMarketCapitalizationAssetPair
    {
        [Key(0)]
        [UsedImplicitly]
        public double Capitalization { get; set; }

        [UsedImplicitly]
        public CachedMarketCapitalizationAssetPair()
        {
        }

        public CachedMarketCapitalizationAssetPair(double capitalization)
        {
            Capitalization = capitalization;
        }
    }
}
