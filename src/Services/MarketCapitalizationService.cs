using System.Linq;
using System.Threading.Tasks;
using Common;
using Core;
using Core.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models.Extensions;
using Lykke.Service.Balances.Client;

namespace Services
{
    public class MarketCapitalizationService : IMarketCapitalizationService
    {
        private readonly IBalancesClient _balancesClient;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly ISrvRatesHelper _srvRatesHelper;

        public MarketCapitalizationService(
            IBalancesClient balancesClient,
            IAssetsServiceWithCache assetsServiceWithCache,
            ISrvRatesHelper srvRatesHelper)
        {
            _balancesClient = balancesClient;
            _assetsServiceWithCache = assetsServiceWithCache;
            _srvRatesHelper = srvRatesHelper;
        }

        public async Task<double?> GetCapitalization(string market)
        {
            double rate = 1;

            if (market != LykkeConstants.LykkeAssetId)
            {
                var assetPairs = await _assetsServiceWithCache.GetAllAssetPairsAsync();
                var pair = assetPairs.PairWithAssets(LykkeConstants.LykkeAssetId, market);

                if (pair == null)
                    return null;

                rate = await _srvRatesHelper.GetRate(market, pair);
            }

            var asset = await _assetsServiceWithCache.TryGetAssetAsync(market);
            
            var balance = (await _balancesClient.GetTotalBalances()).FirstOrDefault(item => item.AssetId == LykkeConstants.LykkeAssetId);

            if (balance == null)
            {
                return null;
            }

            return ((double)balance.Balance * rate).TruncateDecimalPlaces(asset.Accuracy);
        }
    }
}
