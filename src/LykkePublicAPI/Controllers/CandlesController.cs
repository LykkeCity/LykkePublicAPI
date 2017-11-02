using System.Linq;
using System.Threading.Tasks;
using LykkePublicAPI.Models;
using Lykke.Service.CandlesHistory.Client;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class CandlesController : Controller
    {
        private readonly ICandleshistoryservice _candlesService;

        public CandlesController(ICandleshistoryservice candlesService)
        {
            _candlesService = candlesService;
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
        /// <param name="request">Request model</param>
        [HttpPost("history/{assetPairId}")]
        [ProducesResponseType(typeof(CandlesHistoryResponse), 200)]
        [ProducesResponseType(typeof(ApiError), 400)]
        public async Task<IActionResult> GetHistoryCandles([FromRoute]string assetPairId, [FromBody] CandlesHistoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToApiError());
            }

            var history = await _candlesService.GetCandlesHistoryAsync(
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
