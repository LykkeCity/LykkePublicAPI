using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LykkePublicAPI.Models;
using Lykke.Domain.Prices.Contracts;
using Lykke.Domain.Prices.Repositories;
using Microsoft.AspNetCore.Mvc;
using Prices = Lykke.Domain.Prices;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class CandlesController : Controller
    {
        private readonly ICandleHistoryRepository _feedCandlesRepository;

        public CandlesController(ICandleHistoryRepository feedCandlesRepository)
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

            IEnumerable<IFeedCandle> candles = new List<IFeedCandle>();

            try
            {
                candles = await _feedCandlesRepository.GetCandlesAsync(
                    assetPairId,
                    request.Period.Value.ToDomainModel(),
                    priceType: request.Type.HasValue ? request.Type.Value.ToDomainModel() : Prices.PriceType.Ask,
                    from: request.DateFrom.Value,
                    to: request.DateTo.Value);
            }
            catch (AppSettingException)
            {
                // TODO: Log absent connection string for the specified assetPairId
            }

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
