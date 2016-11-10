using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Assets;
using Core.Domain.Exchange;
using Core.Domain.Feed;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class MarketController : Controller
    {
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly IAssetsRepository _assetsRepository;
        private readonly IAssetPairBestPriceRepository _marketProfileRepo;
        //Todo: temporary hardcoded.
        private static readonly string[] _lkkMarkets = { "LKKUSD", "LKKJPY", "LKKGBP", "LKKEUR", "LKKCHF", "ETHLKK", "BTCLKK" };

        public MarketController(IMarketDataRepository marketDataRepository,
            IAssetsRepository assetsRepository, IAssetPairBestPriceRepository marketProfileRepo)
        {
            _marketDataRepository = marketDataRepository;
            _assetsRepository = assetsRepository;
            _marketProfileRepo = marketProfileRepo;
        }

        [HttpGet]
        public async Task<IEnumerable<ApiMarketData>> Get()
        {
            var marketProfile = await _marketProfileRepo.GetAsync();
            var result =
                (await _marketDataRepository.Get24HMarketsAsync()).ToApiModel(marketProfile)
                    .ToList();

            var emptyRecords = _lkkMarkets.Where(x => result.All(y => y.AssetPair != x));
            result.AddRange(emptyRecords.Select(x => new MarketData
            {
                AssetPairId = x,
                Dt = DateTime.UtcNow
            }.ToApiModel(marketProfile.Profile.First(y => y.Asset == x))));

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

                var result = marketData.First(x => x.AssetPairId == market) ??
                             new MarketData { AssetPairId = market, Dt = DateTime.UtcNow};

                return result.ToApiModel(feedData);
            }

            return null;
        }
    }
}
