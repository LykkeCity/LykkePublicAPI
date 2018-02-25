using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Exchange;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.TradesAdapter.Client;
using LykkePublicAPI.Models;
using LykkePublicAPI.Models.Trades;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class TradesController : Controller
    {
        private const int MaxTrades = 500;
        private readonly ITradesCommonRepository _tradesCommonRepository;
        private readonly ITradesAdapterClient _tradesAdapterClient;

        public TradesController(
            ITradesCommonRepository tradesCommonRepository,
            ITradesAdapterClient tradesAdapterClient
            )
        {
            _tradesCommonRepository = tradesCommonRepository;
            _tradesAdapterClient = tradesAdapterClient;
        }

        /// <summary>
        /// Get trade volumes for all available assetpairs
        /// </summary>
        /// <returns></returns>
        [HttpGet("Last/{assetPair}/{n}")]
        public async Task<IActionResult> Get(string assetPair, int n)
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


            var trades = await _tradesCommonRepository.GetLastTrades(assetPair, n);

            return Ok(trades.ToApiModel());
        }
        
        /// <summary>
        /// Provides latest trades for given asset pair
        /// </summary>
        /// <param name="assetPairId">Id of asset pair</param>
        /// <param name="skip">How many items to skip</param>
        /// <param name="take">How many items to take</param>
        /// <returns></returns>
        [HttpGet("{assetPairId}")]
        public async Task<IActionResult> GetTrades(string assetPairId,
            [FromQuery] int skip,
            [FromQuery] int take)
        {
            var trades = await _tradesAdapterClient.GetTradesByAssetPairIdAsync(assetPairId, skip, take);

            if (trades.Error != null)
            {
                return BadRequest(new ApiError
                {
                    Code = ErrorCodes.InvalidInput,
                    Msg = trades.Error.Message
                });
            }
            
            return Ok(trades.Records.Select(x => x.ToResponseModel()));
        }
    }
}
