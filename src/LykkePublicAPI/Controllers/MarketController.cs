using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Domain.Feed;
using Core.Services;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MarketType = Core.Domain.Market.MarketType;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class MarketController : Controller
    {
        private readonly IAssetPairBestPriceRepository _marketProfileRepo;
        private readonly IMarketCapitalizationService _marketCapitalizationService;
        private readonly ICandlesHistoryServiceProvider _candlesHistoryServiceProvider;

        public MarketController(IAssetPairBestPriceRepository marketProfileRepo,
            IMarketCapitalizationService marketCapitalizationService,
            ICandlesHistoryServiceProvider candlesHistoryServiceProvider)
        {
            _marketProfileRepo = marketProfileRepo;
            _marketCapitalizationService = marketCapitalizationService;
            _candlesHistoryServiceProvider = candlesHistoryServiceProvider;
        }

        /// <summary>
        /// Get trade volumes for all available assetpairs
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IEnumerable<ApiMarketData>> Get()
        {
            var marketProfile = await _marketProfileRepo.GetAsync();
            var spotCandlesService = _candlesHistoryServiceProvider.Get(MarketType.Spot);
            
            // TODO: Get 24 hour candles from spot and MT and calculate result
            
//            
//            var result =
//                (await _marketDataRepository.Get24HMarketsAsync()).ToApiModel(marketProfile)
//                    .ToList();
//
//            var assetPairs = (await _assetsService.GetAllAssetPairsAsync()).Where(x => !x.IsDisabled);
//
//            var emptyRecords =
//                assetPairs.Where(
//                    x => result.All(y => y.AssetPair != x.Id) && marketProfile.Profile.Any(z => z.Asset == x.Id));
//            result.AddRange(emptyRecords.Select(x => new MarketData
//            {
//                AssetPairId = x.Id,
//                Dt = DateTime.UtcNow
//            }.ToApiModel(marketProfile.Profile.First(y => y.Asset == x.Id))));

            return new ApiMarketData[0];
        }

        /// <summary>
        /// Get trade volume for asset
        /// </summary>
        [HttpGet("{market}")]
        public async Task<ApiMarketData> Get(string market)
        {
//            var feedData = await _marketProfileRepo.GetAsync(market);
//            if (feedData != null)
//            {
//                var marketData = await _marketDataRepository.Get24HMarketsAsync();
//
//                var result = marketData.FirstOrDefault(x => x.AssetPairId == market) ??
//                             new MarketData { AssetPairId = market, Dt = DateTime.UtcNow};
//
//                return result.ToApiModel(feedData);
//            }
//
//            return null;
            
            return new ApiMarketData();
        }

        /// <summary>
        /// Get trade volume for asset
        /// </summary>
        [HttpGet("capitalization/{market}")]
        public async Task<ApiMarketCapitalizationData> GetMarketCapitalization(string market)
        {
            var amount = await _marketCapitalizationService.GetCapitalization(market);

            return new ApiMarketCapitalizationData {Amount = amount };
        }
    }
}
