using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Services;
using Lykke.Service.MarketProfile.Client;
using Lykke.Service.MarketProfile.Client.Models;

namespace Services
{
    public class MarketProfileService : IMarketProfileService
    {
        private readonly ILykkeMarketProfile _api;

        public MarketProfileService(ILykkeMarketProfile api)
        {
            _api = api;
        }

        public async Task<AssetPairModel> TryGetPairAsync(string assetPairId)
        {
            var result = await _api.ApiMarketProfileByPairCodeGetAsync(assetPairId);
            if (result is AssetPairModel m)
                return m;
            return null;
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
