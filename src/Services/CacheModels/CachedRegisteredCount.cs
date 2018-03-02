using JetBrains.Annotations;
using MessagePack;

namespace Services.CacheModels
{
    [MessagePackObject]
    public class CachedRegisteredCount
    {
        [Key(0)]
        [UsedImplicitly]
        public long Count { get; set; }

        [UsedImplicitly]
        public CachedRegisteredCount()
        {
        }

        public CachedRegisteredCount(long count)
        {
            Count = count;
        }
    }
}
