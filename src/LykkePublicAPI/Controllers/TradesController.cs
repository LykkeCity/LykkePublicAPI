using System.Threading.Tasks;
using Core.Domain.Exchange;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class TradesController : Controller
    {
        private const int MaxTrades = 500;
        private readonly ITradesCommonRepository _tradesCommonRepository;

        public TradesController(ITradesCommonRepository tradesCommonRepository)
        {
            _tradesCommonRepository = tradesCommonRepository;
        }

        /// <summary>
        /// Get trade volumes for all available assetpairs
        /// </summary>
        /// <returns></returns>
        [HttpGet("Last")]
        public async Task<IActionResult> Get([FromQuery] int n)
        {
            if (n <= 0)
                return BadRequest(new ApiError
                {
                    Code = ErrorCodes.InvalidInput,
                    Msg = "N less than zero"
                });

            if (n > MaxTrades)
                return BadRequest(new ApiError
                {
                    Code = ErrorCodes.InvalidInput,
                    Msg = "500 trades max"
                });

            var trades = await _tradesCommonRepository.GetLastTrades(n);

            return Ok(trades.ToApiModel());
        }
    }
}
