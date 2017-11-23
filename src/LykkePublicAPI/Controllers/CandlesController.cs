using System.Linq;
using System.Threading.Tasks;
using Core.Services;
using LykkePublicAPI.Models;
using Lykke.Service.CandlesHistory.Client;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class CandlesController : Controller
    {
        private readonly ICandlesHistoryServiceProvider _candlesServiceProvider;

        public CandlesController(ICandlesHistoryServiceProvider candlesServiceProvider)
        {
            _candlesServiceProvider = candlesServiceProvider;
        }

        /// <summary>
        /// Get candles for specified period and asset pair
        /// </summary>
        /// <remarks>
        /// Available markets
        ///     Spot,
        ///     Mt
        /// Available period values
        ///     Sec,
        ///     Minute,
        ///     Min5,
        ///     Min15,
        ///     Min30,
        ///     Hour,
        ///     Hour4,
        ///     Hour6,
        ///     Hour12,
        ///     Day,
        ///     Week,
        ///     Month
        /// </remarks>
        /// <param name="assetPairId">Asset pair Id</param>
        /// <param name="market">Market type</param>
        /// <param name="request">Request model</param>
        [HttpPost("history/{assetPairId}/{market?}")]
        [ProducesResponseType(typeof(CandlesHistoryResponse), 200)]
        [ProducesResponseType(typeof(ApiError), 400)]
        public async Task<IActionResult> GetHistoryCandles(
            [FromRoute] string assetPairId,
            [FromRoute] MarketType? market,
            [FromBody] CandlesHistoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToApiError());
            }

            var candlesService = _candlesServiceProvider.TryGet((market ?? MarketType.Spot).ToDomain());

            if (candlesService == null)
            {
                return BadRequest(new ApiError
                {
                    Code = ErrorCodes.InvalidInput,
                    Msg = "Invalid market"
                });
            }

            var history = await candlesService.GetCandlesHistoryAsync(
                assetPairId,
                request.Type.Value.ToCandlesHistoryServiceModel(),
                request.Period.Value.ToCandlesHistoryServiceApiModel(),
                request.DateFrom.Value,
                request.DateTo.Value);

            var response = new CandlesHistoryResponse
            {
                AssetPair = assetPairId,
                Period = request.Period.Value,
                DateFrom = request.DateFrom.Value,
                DateTo = request.DateTo.Value,
                Type = request.Type.Value,
                Data = history.History.ToApiModel().ToList()
            };

            return Ok(response);
        }
    }
}
