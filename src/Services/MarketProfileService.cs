using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Services;
using Lykke.MarketProfileService.Client;
using Lykke.MarketProfileService.Client.Models;

namespace Services
{
    public class MarketProfileService : IMarketProfileService
    {
        private readonly ILykkeMarketProfileServiceAPI _api;

        public MarketProfileService(ILykkeMarketProfileServiceAPI api)
        {
            _api = api;
        }

        public async Task<AssetPairModel> TryGetPairAsync(string assetPairId)
        {
            return await _api.TryGetAssetPairAsync(assetPairId);
        }

        public async Task<IEnumerable<AssetPairModel>> GetAllPairsAsync()
        {
            return await _api.ApiMarketProfileGetAsync();
        }

        public void Dispose()
        {
            _api?.Dispose();
        }
    }
}
