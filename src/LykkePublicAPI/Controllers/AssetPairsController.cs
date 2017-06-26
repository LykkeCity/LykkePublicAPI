using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Feed;
using Core.Services;
using Lykke.Domain.Prices.Repositories;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.Assets.Client.Custom;
using Prices = Lykke.Domain.Prices;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class AssetPairsController : Controller
    {
        private readonly ICachedAssetsService _assetsService;
        private readonly ICandleHistoryRepository _feedCandlesRepository;
        private readonly IFeedHistoryRepository _feedHistoryRepository;
        private readonly IMarketProfileService _marketProfileService;

        public AssetPairsController(
            ICachedAssetsService assetsService,
            ICandleHistoryRepository feedCandlesRepository, 
            IFeedHistoryRepository feedHistoryRepository,
            IMarketProfileService marketProfileService)
        {
            _assetsService = assetsService;
            _feedCandlesRepository = feedCandlesRepository;
            _feedHistoryRepository = feedHistoryRepository;
            _marketProfileService = marketProfileService;
        }

        /// <summary>
        /// Get all asset pairs rates
        /// </summary>
        [HttpGet("rate")]
        public async Task<IEnumerable<ApiAssetPairRateModel>> GetRate()
        {
            var assetPairsIds = (await _assetsService.GetAllAssetPairsAsync())
                .Where(x => !x.IsDisabled)
                .Select(x => x.Id)
                .ToArray();

            var marketProfile = (await _marketProfileService.GetAllPairsAsync())
                .Where(x => assetPairsIds.Contains(x.AssetPair))
                .Select(pair => pair.ToApiModel());

            return marketProfile;
        }

        /// <summary>
        /// Get rates for asset pair
        /// </summary>
        [HttpGet("rate/{assetPairId}")]
        public async Task<ApiAssetPairRateModel> GetRate(string assetPairId)
        {
            return (await _marketProfileService.TryGetPairAsync(assetPairId))?.ToApiModel();
        }

        /// <summary>
        /// Get asset pairs dictionary
        /// </summary>
        [HttpGet("dictionary")]
        public async Task<IEnumerable<ApiAssetPair>> GetDictionary()
        {
            var pairs = (await _assetsService.GetAllAssetPairsAsync()).Where(x => !x.IsDisabled);

            return pairs.ToApiModel();
        }

        /// <summary>
        /// Get rates for specified period
        /// </summary>
        /// <remarks>
        /// Available period values
        ///  
        ///     "Sec",
        ///     "Minute",
        ///     "Hour",
        ///     "Day",
        ///     "Month",
        /// 
        /// </remarks>
        [HttpPost("rate/history")]
        [ProducesResponseType(typeof(IEnumerable<ApiAssetPairRateModel>), 200)]
        [ProducesResponseType(typeof(ApiError), 400)]
        public async Task<IActionResult> GetHistoryRate([FromBody] AssetPairsRateHistoryRequest request)
        {
            //if (request.AssetPairIds.Length > 10)
            //    return
            //        BadRequest(new ApiError {Code = ErrorCodes.InvalidInput, Msg = "Maximum 10 asset pairs allowed" });

            if (request.Period != Period.Day)
                return
                    BadRequest(new ApiError { Code = ErrorCodes.InvalidInput, Msg = "Sorry, only day candles are available (temporary)." });

            var pairs = (await _assetsService.GetAllAssetPairsAsync()).Where(x => !x.IsDisabled);

            if (request.AssetPairIds.Any(x => !pairs.Select(y => y.Id).Contains(x)))
                return
                    BadRequest(new ApiError {Code = ErrorCodes.InvalidInput, Msg = "Unkown asset pair id present"});

            //var candlesTasks = new List<Task<CandleWithPairId>>();

            var candles = new List<CandleWithPairId>();
            var result = new List<ApiAssetPairHistoryRateModel>();

            foreach (var pairId in request.AssetPairIds)
            {
                var askFeed = _feedHistoryRepository.GetСlosestAvailableAsync(pairId, TradePriceType.Ask, request.DateTime);
                var bidFeed = _feedHistoryRepository.GetСlosestAvailableAsync(pairId, TradePriceType.Bid, request.DateTime);

                var askCandle = (await askFeed)?.ToCandleWithPairId();
                var bidCandle = (await bidFeed)?.ToCandleWithPairId();

                if (askCandle != null && bidCandle != null)
                {
                    candles.Add(askCandle);
                    candles.Add(bidCandle);
                }
                else
                {
                    //add empty candles
                    result.Add(new ApiAssetPairHistoryRateModel {Id = pairId});
                }

                //candlesTasks.Add(_feedCandlesRepository.ReadCandleAsync(pairId, request.Period.ToDomainModel(),
                //    true, request.DateTime).ContinueWith(task => new CandleWithPairId
                //{
                //    AssetPairId = pairId,
                //    Candle = task.Result
                //}));

                //candlesTasks.Add(_feedCandlesRepository.ReadCandleAsync(pairId, request.Period.ToDomainModel(),
                //    false, request.DateTime).ContinueWith(task => new CandleWithPairId
                //{
                //    AssetPairId = pairId,
                //    Candle = task.Result
                //}));
            }

            //var candles = await Task.WhenAll(candlesTasks);

            result.AddRange(candles.ToApiModel());

            return Ok(result);
        }


        /// <summary>
        /// Get rates for specified period and asset pair
        /// </summary>
        /// <remarks>
        /// Available period values
        ///  
        ///     "Sec",
        ///     "Minute",
        ///     "Hour",
        ///     "Day",
        ///     "Month",
        /// 
        /// </remarks>
        /// <param name="assetPairId">Asset pair Id</param>
        [HttpPost("rate/history/{assetPairId}")]
        public async Task<ApiAssetPairHistoryRateModel> GetHistoryRate([FromRoute]string assetPairId,
            [FromBody] AssetPairRateHistoryRequest request)
        {
            IFeedCandle buyCandle = null;
            IFeedCandle sellCandle = null;
            try
            {
                buyCandle = await _feedCandlesRepository.GetCandleAsync(assetPairId, request.Period.ToDomainModel(),
                    Prices.PriceType.Bid, request.DateTime);

                sellCandle = await _feedCandlesRepository.GetCandleAsync(assetPairId, request.Period.ToDomainModel(),
                    Prices.PriceType.Ask, request.DateTime);
            }
            catch (AppSettingException)
            {
                // TODO: Log absent connection string for the specified assetPairId
            }

            return Convertions.ToApiModel(assetPairId, buyCandle, sellCandle);
        }
    }
}
