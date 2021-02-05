using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Antares.Service.MarketProfile.Client;
using Antares.Service.MarketProfile.LykkeClient;
using Antares.Service.MarketProfile.LykkeClient.Models;
using Core.Services;
using Lykke.Job.MarketProfile.Contract;

namespace Services
{
    public class MarketProfileService : IMarketProfileService
    {
        private readonly ILykkeMarketProfile _api;
        private readonly IMarketProfileServiceClient _client;

        public MarketProfileService(IMarketProfileServiceClient client)
        {
            _api = client.HttpClient;
            _client = client;
        }

        public async Task<AssetPairModel> TryGetPairAsync(string assetPairId)
        {
            var assetPair = _client.Get(assetPairId);

            if (assetPair != null)
                return MapToModel(assetPair);

            var result = await _api.ApiMarketProfileByPairCodeGetAsync(assetPairId);
            if (result is AssetPairModel m)
                return m;

            return null;
        }

        private static AssetPairModel MapToModel(IAssetPair assetPair)
        {
            return new AssetPairModel(assetPair.AssetPair, assetPair.BidPrice, assetPair.AskPrice, assetPair.BidPriceTimestamp, assetPair.AskPriceTimestamp);
        }

        public Task<IEnumerable<AssetPairModel>> GetAllPairsAsync()
        {
            var allPairs = _client.GetAll();
            return Task.FromResult(allPairs.Select<IAssetPair, AssetPairModel>(MapToModel));
        }

        public void Dispose()
        {
            _api?.Dispose();
        }
    }
}
