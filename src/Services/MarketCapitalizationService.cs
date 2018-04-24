using System.Linq;
using System.Threading.Tasks;
using Common;
using Core;
using Core.Services;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.Balances.Client;

namespace Services
{
    public class MarketCapitalizationService : IMarketCapitalizationService
    {
        private readonly IBalancesClient _balancesClient;
        private readonly ICachedAssetsService _assetsService;
        private readonly ISrvRatesHelper _srvRatesHelper;

        public MarketCapitalizationService(
            IBalancesClient balancesClient,
            ICachedAssetsService assetsService,
            ISrvRatesHelper srvRatesHelper)
        {
            _balancesClient = balancesClient;
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
            
            var balance = (await _balancesClient.GetTotalBalances()).FirstOrDefault(item => item.AssetId == LykkeConstants.LykkeAssetId);

            if (balance == null)
            {
                return null;
            }

            return ((double)balance.Balance * rate).TruncateDecimalPlaces(asset.Accuracy);
        }
    }
}
