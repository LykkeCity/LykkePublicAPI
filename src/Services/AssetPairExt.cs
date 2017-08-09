using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Assets.Client.Custom;

namespace Services
{
    internal static class AssetPairExt
    {
        public static IAssetPair PairWithAssets(this IEnumerable<IAssetPair> src, string assetId1, string assetId2)
        {
            return src.FirstOrDefault(assetPair =>
                (assetPair.BaseAssetId == assetId1 && assetPair.QuotingAssetId == assetId2) ||
                (assetPair.BaseAssetId == assetId2 && assetPair.QuotingAssetId == assetId1)
            );
        }

        public static bool IsInverted(this IAssetPair assetPair, string targetAsset)
        {
            return assetPair.QuotingAssetId == targetAsset;
        }
    }
}