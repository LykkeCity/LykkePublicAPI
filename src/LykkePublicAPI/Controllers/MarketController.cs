using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Services;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class MarketController : Controller
    {
        private readonly IMarketProfileService _marketProfileService;
        private readonly IMarketCapitalizationService _marketCapitalizationService;
        private readonly IMarketTradingDataService _marketTradingDataService;

        public MarketController(
            IMarketProfileService marketProfileService,
            IMarketCapitalizationService marketCapitalizationService,
            IMarketTradingDataService marketTradingDataService)
        {
            _marketProfileService = marketProfileService;
            _marketCapitalizationService = marketCapitalizationService;
            _marketTradingDataService = marketTradingDataService;
        }

        /// <summary>
        /// Get trade volumes for all available assetpairs
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IEnumerable<ApiMarketData>> Get()
        {
            var marketProfileTask = _marketProfileService.GetAllPairsAsync();
            var tradingDataTask = _marketTradingDataService.GetAllPairsAsync();

            var marketProfile = await marketProfileTask;
            var tradingData = await tradingDataTask;

            var result = new Dictionary<string, ApiMarketData>();

            if (marketProfile != null)
            {
                foreach (var assetProfile in marketProfile)
                {
                    result[assetProfile.AssetPair] = new ApiMarketData
                    {
                        AssetPair = assetProfile.AssetPair,
                        Ask = assetProfile.AskPrice,
                        Bid = assetProfile.BidPrice
                    };
                }
            }

            if (tradingData != null)
            {
                foreach (var assetTradingData in tradingData)
                {
                    if (!result.TryGetValue(assetTradingData.AssetPair, out var marketData))
                    {
                        marketData = new ApiMarketData
                        {
                            AssetPair = assetTradingData.AssetPair
                        };

                        result.Add(assetTradingData.AssetPair, marketData);
                    }

                    marketData.LastPrice = assetTradingData.LastTradePrice;
                    marketData.Volume24H = assetTradingData.Volume24;
                }
            }

            return result.Values;
        }

        /// <summary>
        /// Get trade volume for asset pair
        /// </summary>
        [HttpGet("{assetPair}")]
        public async Task<ApiMarketData> Get(string assetPair)
        {
            var marketProfileTask = _marketProfileService.TryGetPairAsync(assetPair);
            var tradingDataTask = _marketTradingDataService.TryGetPairAsync(assetPair);

            var marketProfile = await marketProfileTask;
            var tradingData = await tradingDataTask;

            var result = new ApiMarketData
            {
                AssetPair = assetPair
            };

            if (marketProfile != null)
            {
                result.Ask = marketProfile.AskPrice;
                result.Bid = marketProfile.BidPrice;
            }

            if (tradingData != null)
            {
                result.LastPrice = tradingData.LastTradePrice;
                result.Volume24H = tradingData.Volume24;
            }

            return result;

        }

        /// <summary>
        /// Get trade volume for asset
        /// </summary>
        [HttpGet("capitalization/{market}")]
        public async Task<ApiMarketCapitalizationData> GetMarketCapitalization(string market)
        {
            var amount = await _marketCapitalizationService.GetCapitalization(market);

            return new ApiMarketCapitalizationData
            {
                Amount = amount ?? 0
            };
        }
    }
}
