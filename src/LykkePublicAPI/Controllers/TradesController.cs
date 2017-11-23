using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Exchange;
using Lykke.Service.Assets.Client.Custom;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class TradesController : Controller
    {
        private const int MaxTrades = 500;
        private readonly ITradesCommonRepository _tradesCommonRepository;
        private readonly ICachedAssetsService _cachedAssetsService;

        public TradesController(
            ITradesCommonRepository tradesCommonRepository,
            ICachedAssetsService cachedAssetsService
            )
        {
            _tradesCommonRepository = tradesCommonRepository;
            _cachedAssetsService = cachedAssetsService;
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
            var assetPairs = await _cachedAssetsService.GetAllAssetPairsAsync();

            return Ok(trades.ToApiModel(assetPairs));
        }
    }
}
