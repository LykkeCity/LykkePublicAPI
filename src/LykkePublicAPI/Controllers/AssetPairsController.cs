using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Core.Domain.Assets;
using Core.Domain.Candles;
using Core.Domain.Feed;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class AssetPairsController : Controller
    {
        private readonly IAssetPairBestPriceRepository _assetPairBestPriceRepository;
        private readonly CachedDataDictionary<string, IAssetPair> _assetPairDictionary;
        private readonly IFeedCandlesRepository _feedCandlesRepository;

        public AssetPairsController(IAssetPairBestPriceRepository assetPairBestPriceRepository,
            CachedDataDictionary<string, IAssetPair> assetPairDictionary,
            IFeedCandlesRepository feedCandlesRepository)
        {
            _assetPairBestPriceRepository = assetPairBestPriceRepository;
            _assetPairDictionary = assetPairDictionary;
            _feedCandlesRepository = feedCandlesRepository;
        }

        /// <summary>
        /// Get all asset pairs rates
        /// </summary>
        [HttpGet("rate")]
        public async Task<IEnumerable<ApiAssetPairRateModel>> GetRate()
        {
            var marketProfile = await _assetPairBestPriceRepository.GetAsync();
            return marketProfile.ToApiModel();
        }

        /// <summary>
        /// Get rates for asset pair
        /// </summary>
        [HttpGet("rate/{assetPairId}")]
        public async Task<ApiAssetPairRateModel> GetRate(string assetPairId)
        {
            return (await _assetPairBestPriceRepository.GetAsync(assetPairId))?.ToApiModel();
        }

        /// <summary>
        /// Get asset pairs dictionary
        /// </summary>
        [HttpGet("dictionary")]
        public async Task<IEnumerable<ApiAssetPair>> GetDictionary()
        {
            var pairs = (await _assetPairDictionary.Values()).Where(x => !x.IsDisabled);

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
            if (request.AssetPairIds.Length > 10)
                return
                    BadRequest(Json(new ApiError {Code = ErrorCodes.InvalidInput, Msg = "Maximum 10 asset pairs allowed" }));

            var pairs = (await _assetPairDictionary.Values()).Where(x => !x.IsDisabled);

            if (request.AssetPairIds.Any(x => !pairs.Select(y => y.Id).Contains(x)))
                return
                    BadRequest(Json(new ApiError {Code = ErrorCodes.InvalidInput, Msg = "Unkown asset pair id present"}));

            var candlesTasks = new List<Task<CandleWithPairId>>();

            foreach (var pairId in request.AssetPairIds)
            {
                candlesTasks.Add(_feedCandlesRepository.ReadCandleAsync(pairId, request.Period.ToDomainModel(),
                    true, request.DateTime).ContinueWith(task => new CandleWithPairId
                {
                    AssetPairId = pairId,
                    Candle = task.Result
                }));

                candlesTasks.Add(_feedCandlesRepository.ReadCandleAsync(pairId, request.Period.ToDomainModel(),
                    false, request.DateTime).ContinueWith(task => new CandleWithPairId
                {
                    AssetPairId = pairId,
                    Candle = task.Result
                }));
            }

            var candles = await Task.WhenAll(candlesTasks);

            return Ok(candles.ToApiModel());
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
        public async Task<ApiAssetPairRateModel> GetHistoryRate([FromRoute]string assetPairId,
            [FromBody] AssetPairRateHistoryRequest request)
        {
            var buyCandle = _feedCandlesRepository.ReadCandleAsync(assetPairId, request.Period.ToDomainModel(),
                true, request.DateTime);

            var sellCandle = _feedCandlesRepository.ReadCandleAsync(assetPairId, request.Period.ToDomainModel(),
                false, request.DateTime);

            return Convertions.ToApiModel(assetPairId, await buyCandle, await sellCandle);
        }
    }
}
