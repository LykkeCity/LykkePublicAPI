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
            var tradingDataVolumesTask = _marketTradingDataService.TryGetAllPairsVolumeAsync();
            var tradingDataLastTradePricesTask = _marketTradingDataService.TryGetAllPairsLastTradePriceAsync();

            var marketProfile = await marketProfileTask;
            var tradingDataVolumes = await tradingDataVolumesTask;
            var tradingDataLastTradePrices = await tradingDataLastTradePricesTask;

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

            if (tradingDataVolumes != null)
            {
                foreach (var assetTradingDataVolume in tradingDataVolumes)
                {
                    if (!result.TryGetValue(assetTradingDataVolume.AssetPair, out var marketData))
                    {
                        marketData = new ApiMarketData
                        {
                            AssetPair = assetTradingDataVolume.AssetPair
                        };

                        result.Add(assetTradingDataVolume.AssetPair, marketData);
                    }

                    marketData.Volume24H = assetTradingDataVolume.Value;
                }
            }

            if (tradingDataLastTradePrices != null)
            {
                foreach (var tradingDataLastTradePrice in tradingDataLastTradePrices)
                {
                    if (!result.TryGetValue(tradingDataLastTradePrice.AssetPair, out var marketData))
                    {
                        marketData = new ApiMarketData
                        {
                            AssetPair = tradingDataLastTradePrice.AssetPair
                        };

                        result.Add(tradingDataLastTradePrice.AssetPair, marketData);
                    }

                    marketData.LastPrice = tradingDataLastTradePrice.Value;
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
            var tradingDataVolumeTask = _marketTradingDataService.TryGetPairVolumeAsync(assetPair);
            var tradingDataLastTradePriceTask = _marketTradingDataService.TryGetPairLastTradePriceAsync(assetPair);

            var marketProfile = await marketProfileTask;
            var tradingDataVolume = await tradingDataVolumeTask;
            var tradingDataLastTradePrice = await tradingDataLastTradePriceTask;

            var result = new ApiMarketData
            {
                AssetPair = assetPair
            };

            if (marketProfile != null)
            {
                result.Ask = marketProfile.AskPrice;
                result.Bid = marketProfile.BidPrice;
            }

            if (tradingDataVolume != null)
                result.Volume24H = tradingDataVolume.Value;

            if (tradingDataLastTradePrice != null)
                result.LastPrice = tradingDataLastTradePrice.Value;

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
