using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Core.Domain.Assets;
using Core.Domain.Exchange;
using Core.Domain.Feed;
using Core.Services;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class MarketController : Controller
    {
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly IAssetPairBestPriceRepository _marketProfileRepo;
        private readonly CachedDataDictionary<string, IAssetPair> _assetPairsDictionary;
        private readonly IMarketCapitalizationService _marketCapitalizationService;

        public MarketController(IMarketDataRepository marketDataRepository,
            IAssetPairBestPriceRepository marketProfileRepo,
            CachedDataDictionary<string, IAssetPair> assetPairsDictionary,
            IMarketCapitalizationService marketCapitalizationService)
        {
            _marketDataRepository = marketDataRepository;
            _marketProfileRepo = marketProfileRepo;
            _assetPairsDictionary = assetPairsDictionary;
            _marketCapitalizationService = marketCapitalizationService;
        }

        /// <summary>
        /// Get trade volumes for all available assetpairs
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IEnumerable<ApiMarketData>> Get()
        {
            var marketProfile = await _marketProfileRepo.GetAsync();
            var result =
                (await _marketDataRepository.Get24HMarketsAsync()).ToApiModel(marketProfile)
                    .ToList();

            var assetPairs = (await _assetPairsDictionary.Values()).Where(x => !x.IsDisabled);

            var emptyRecords =
                assetPairs.Where(
                    x => result.All(y => y.AssetPair != x.Id) && marketProfile.Profile.Any(z => z.Asset == x.Id));
            result.AddRange(emptyRecords.Select(x => new MarketData
            {
                AssetPairId = x.Id,
                Dt = DateTime.UtcNow
            }.ToApiModel(marketProfile.Profile.First(y => y.Asset == x.Id))));

            return result;
        }

        /// <summary>
        /// Get trade volume for asset
        /// </summary>
        [HttpGet("{market}")]
        public async Task<ApiMarketData> Get(string market)
        {
            var feedData = await _marketProfileRepo.GetAsync(market);
            if (feedData != null)
            {
                var marketData = await _marketDataRepository.Get24HMarketsAsync();

                var result = marketData.FirstOrDefault(x => x.AssetPairId == market) ??
                             new MarketData { AssetPairId = market, Dt = DateTime.UtcNow};

                return result.ToApiModel(feedData);
            }

            return null;
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
