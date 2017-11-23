using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Assets.Client.Custom;

namespace LykkePublicAPI.Extensions
{
    public static class AssetPairExtensions
    {
        public static string GetAssetPairId(this IEnumerable<IAssetPair> src, string baseAssetId, string quoteAssetId)
        {
            return src.FirstOrDefault(assetPair => assetPair.BaseAssetId == baseAssetId && assetPair.QuotingAssetId == quoteAssetId)?.Id;
        }
    }
}
