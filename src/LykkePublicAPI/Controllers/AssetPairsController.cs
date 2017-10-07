using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Feed;
using Core.Services;
using Lykke.Domain.Prices;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.Assets.Client.Custom;
using Prices = Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Client;
using PriceType = Lykke.Service.CandlesHistory.Client.Models.PriceType;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class AssetPairsController : Controller
    {
        private readonly ICachedAssetsService _assetsService;
        private readonly ICandleshistoryservice _candlesHistoryService;
        private readonly IFeedHistoryRepository _feedHistoryRepository;
        private readonly IMarketProfileService _marketProfileService;

        public AssetPairsController(
            ICachedAssetsService assetsService,
            ICandleshistoryservice candlesHistoryService, 
            IFeedHistoryRepository feedHistoryRepository,
            IMarketProfileService marketProfileService)
        {
            _assetsService = assetsService;
            _candlesHistoryService = candlesHistoryService;
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

                //candlesTasks.Add(_candlesHistoryService.ReadCandleAsync(pairId, request.Period.ToCandlesHistoryServiceModel(),
                //    true, request.DateTime).ContinueWith(task => new CandleWithPairId
                //{
                //    AssetPairId = pairId,
                //    Candle = task.Result
                //}));

                //candlesTasks.Add(_candlesHistoryService.ReadCandleAsync(pairId, request.Period.ToCandlesHistoryServiceModel(),
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
        /// <param name="request">Request model</param>
        [HttpPost("rate/history/{assetPairId}")]
        public async Task<ApiAssetPairHistoryRateModel> GetHistoryRate([FromRoute]string assetPairId,
            [FromBody] AssetPairRateHistoryRequest request)
        {
            var timeInterval = request.Period.ToDomainModel();
            // HACK: Day and month ticks are starts from 1, AddIntervalTicks takes this into account,
            // so compensate it here
            var toDate = timeInterval == TimeInterval.Day || timeInterval == TimeInterval.Month
                ? request.DateTime.AddIntervalTicks(2, request.Period.ToDomainModel())
                : request.DateTime.AddIntervalTicks(1, request.Period.ToDomainModel());

            var buyHistory = await _candlesHistoryService.GetCandlesHistoryAsync(assetPairId, PriceType.Bid, request.Period.ToCandlesHistoryServiceApiModel(), request.DateTime, toDate);
            var sellHistory = await _candlesHistoryService.GetCandlesHistoryAsync(assetPairId, PriceType.Ask, request.Period.ToCandlesHistoryServiceApiModel(), request.DateTime, toDate);

            var buyCandle = buyHistory.History.SingleOrDefault();
            var sellCandle = sellHistory.History.SingleOrDefault();

            return Convertions.ToApiModel(assetPairId, buyCandle, sellCandle);
        }
    }
}
