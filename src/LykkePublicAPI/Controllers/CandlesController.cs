using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Core.Domain.Assets;
using Core.Domain.Candles;
using Core.Domain.Feed;
using Core.Feed;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class CandlesController : Controller
    {
        private readonly IFeedCandlesRepository _feedCandlesRepository;

        public CandlesController(IFeedCandlesRepository feedCandlesRepository)
        {
            _feedCandlesRepository = feedCandlesRepository;
        }

        /// <summary>
        /// Get candles for specified period and asset pair
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
        [HttpPost("candle/history/{assetPairId}")]
        [ProducesResponseType(typeof(IEnumerable<ApiCandleWithPair>), 200)]
        [ProducesResponseType(typeof(ApiError), 400)]
        public async Task<IActionResult> GetHistoryCandles([FromRoute]string assetPairId, [FromBody] AssetPairCandlesHistoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToApiError());
            }

            IEnumerable<IFeedCandle> candles = await _feedCandlesRepository.ReadCandlesAsync(
                assetPairId, 
                request.Period.Value.ToDomainModel(), 
                request.DateFrom.Value, 
                request.DateTo.Value, 
                isBuy: true);

            return Ok(candles.ToApiModel(assetPairId));
        }
    }
}
