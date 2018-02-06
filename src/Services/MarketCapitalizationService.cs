using System.Linq;
using System.Threading.Tasks;
using Common;
using Core;
using Core.Domain.Accounts;
using Core.Services;
using Lykke.Service.Assets.Client.Custom;

namespace Services
{
    public class MarketCapitalizationService : IMarketCapitalizationService
    {
        private readonly IWalletsRepository _walletsRepository;
        private readonly ICachedAssetsService _assetsService;
        private readonly ISrvRatesHelper _srvRatesHelper;

        public MarketCapitalizationService(
            IWalletsRepository walletsRepository,
            ICachedAssetsService assetsService,
            ISrvRatesHelper srvRatesHelper)
        {
            _walletsRepository = walletsRepository;
            _assetsService = assetsService;
            _srvRatesHelper = srvRatesHelper;
        }

        public async Task<double?> GetCapitalization(string market)
        {
            double rate = 1;

            if (market != LykkeConstants.LykkeAssetId)
            {
                var assetPairs = await _assetsService.GetAllAssetPairsAsync();
                var pair = assetPairs.PairWithAssets(LykkeConstants.LykkeAssetId, market);

                if (pair == null)
                    return null;

                rate = await _srvRatesHelper.GetRate(market, pair);
            }

            var asset = await _assetsService.TryGetAssetAsync(market);
            
            var amount = 0d;
            var chunksCount = 0;

            await _walletsRepository.GetWalletsByChunkAsync(pairs =>
            {
                var c = pairs
                    .Select(x => x.Value?.FirstOrDefault(y => y.AssetId == LykkeConstants.LykkeAssetId))
                    .Sum(x => x?.Balance ?? 0);

                amount += c;
                ++chunksCount;

                return Task.CompletedTask;
            });

            if (chunksCount == 0)
            {
                return null;
            }

            return (amount * rate).TruncateDecimalPlaces(asset.Accuracy);
        }
    }
}
