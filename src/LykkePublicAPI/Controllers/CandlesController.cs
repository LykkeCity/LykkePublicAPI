using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Candles;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Lykke.Domain.Prices.Contracts;

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
        [HttpPost("history/{assetPairId}")]
        [ProducesResponseType(typeof(CandlesHistoryResponse), 200)]
        [ProducesResponseType(typeof(ApiError), 400)]
        public async Task<IActionResult> GetHistoryCandles([FromRoute]string assetPairId, [FromBody] CandlesHistoryRequest request)
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
                isBuy: request.Type == PriceType.Bid);

            var response = new CandlesHistoryResponse()
            {
                AssetPair = assetPairId,
                Period = request.Period.Value,
                DateFrom = request.DateFrom.Value,
                DateTo = request.DateTo.Value,
                Type = request.Type.Value,
                Data = candles.ToApiModel().ToList()
            };

            return Ok(response);
        }
    }
}
