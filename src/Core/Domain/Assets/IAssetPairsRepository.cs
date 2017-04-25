using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Domain.Assets
{
    public interface IAssetPair
    {
        string Id { get; }
        string Name { get; }
        string BaseAssetId { get; }
        string QuotingAssetId { get; }
        int Accuracy { get; }
        int InvertedAccuracy { get; }
        string Source { get; }
        string Source2 { get; }
        bool IsDisabled { get; }
    }

    public class AssetPair : IAssetPair
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BaseAssetId { get; set; }
        public string QuotingAssetId { get; set; }
        public int Accuracy { get; set; }
        public int InvertedAccuracy { get; set; }
        public string Source { get; set; }
        public string Source2 { get; set; }
        public bool IsDisabled { get; set; }


        public static AssetPair CreateDefault()
        {
            return new AssetPair
            {
                Accuracy = 5,
                IsDisabled = false
            };
        }

    }

    public interface IAssetPairsRepository
    {
        Task<IEnumerable<IAssetPair>> GetAllAsync();
        Task<IAssetPair> GetAsync(string id);
        Task AddAsync(IAssetPair assetPair);
        Task EditAsync(string id, IAssetPair assetPair);

    }


    public static class AssetPairExt
    {
        public static int Multiplier(this IAssetPair assetPair)
        {
            return (int)Math.Pow(10, assetPair.Accuracy);
        }

        public static string RateToString(this double src, IAssetPair assetPair)
        {
            var mask = "0." + new string('#', assetPair.Accuracy);
            return src.ToString(mask);
        }

        public static IEnumerable<IAssetPair> WhichHaveAssets(this IEnumerable<IAssetPair> src, params string[] assetIds)
        {
            return src.Where(assetPair => assetIds.Contains(assetPair.BaseAssetId) || assetIds.Contains(assetPair.QuotingAssetId));
        }

        public static IEnumerable<IAssetPair> WhichConsistsOfAssets(this IEnumerable<IAssetPair> src, params string[] assetIds)
        {
            return src.Where(assetPair => assetIds.Contains(assetPair.BaseAssetId) && assetIds.Contains(assetPair.QuotingAssetId));
        }

        public static IAssetPair PairWithAssets(this IEnumerable<IAssetPair> src, string assetId1, string assetId2)
        {
            return src.FirstOrDefault(assetPair =>
            (assetPair.BaseAssetId == assetId1 && assetPair.QuotingAssetId == assetId2) ||
            (assetPair.BaseAssetId == assetId2 && assetPair.QuotingAssetId == assetId1)
            );
        }

        public static async Task<IAssetPair> GetAsync(this IAssetPairsRepository assetPairsRepository, string assetId1, string assetId2)
        {
            var assetPairs = await assetPairsRepository.GetAllAsync();
            return assetPairs.FirstOrDefault(itm =>
                (itm.BaseAssetId == assetId1 && itm.QuotingAssetId == assetId2) ||
                (itm.BaseAssetId == assetId2 && itm.QuotingAssetId == assetId1));
        }

        public static bool IsInverted(this IAssetPair assetPair, string targetAsset)
        {
            return assetPair.QuotingAssetId == targetAsset;
        }
    }

}