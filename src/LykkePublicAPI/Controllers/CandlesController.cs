using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Services;
using LykkePublicAPI.Models;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.CandlesHistory.Client.Models;
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
        /// Get list of supported asset pairs for the given market type.
        /// </summary>
        /// <param name="market">The market type. Acceptable values: Spot, Mt.</param>
        [HttpGet("{market}/available")]
        [ProducesResponseType(typeof(CandlesHistoryResponse<ApiCandle>), 200)]
        [ProducesResponseType(typeof(ApiError), 400)]
        public async Task<IActionResult> GetAvailableAssets(MarketType market)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToApiError());
            }

            var candlesService = _candlesServiceProvider.TryGet(market.ToDomain());

            if (candlesService == null)
            {
                return BadRequest(new ApiError
                {
                    Code = ErrorCodes.InvalidInput,
                    Msg = "Invalid market"
                });
            }

            var response = await candlesService.GetAvailableAssetPairsAsync();
           
            return Ok(response);
        }

        /// <summary>
        /// [Obsolete] Get candles for specified period and asset pair. Please, use the -GET- method instead of this.
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
        [Obsolete("This method will be removed in future versions. Please, migrate your implementation on using the [GET] alternative method.")]
        [HttpPost("history/{assetPairId}/{market?}")]
        [ProducesResponseType(typeof(CandlesHistoryResponse<ApiCandle>), 200)]
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

            CandlesHistoryResponseModel history;
            
            try
            {
                history = await candlesService.GetCandlesHistoryAsync(
                    assetPairId,
                    request.Type.Value.ToCandlesHistoryServiceModel(),
                    request.Period.Value.ToCandlesHistoryServiceApiModel(),
                    request.DateFrom.Value,
                    request.DateTo.Value);
            }
            catch (Lykke.Service.CandlesHistory.Client.Custom.ErrorResponseException e)
            {
                var errorMessage = e.Error.ErrorMessages != null && e.Error.ErrorMessages.Any()
                    ? string.Join(",", e.Error.ErrorMessages.Values.SelectMany(msg => msg).ToArray())
                    : "History for asset pair is not available";
                
                return BadRequest(new ApiError
                {
                    Code = ErrorCodes.InvalidInput,
                    Msg = errorMessage
                });
            }

            var response = new CandlesHistoryResponse<ApiCandle>
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

        /// <summary>
        /// Get candles for specified period and asset pair
        /// </summary>
        /// <param name="market">The market type. Acceptable values: Spot, Mt.</param>
        /// <param name="assetPair">The asset pair Id.</param>
        /// <param name="period">The time period. Acceptable values: Sec, Minute, Min5, Min15, Min30, Hour, Hour4, Hour6, Hour12, Day, Week, Month.</param>
        /// <param name="type">The price type. Acceptable values: Bid, Ask, Mid, Trades.</param>
        /// <param name="from">The request's starting date and time.</param>
        /// <param name="to">The request's finishing date and time.</param>
        [HttpGet("history/{market}/{assetPair}/{period}/{type}/{from}/{to}")]
        [ProducesResponseType(typeof(CandlesHistoryResponse<ApiCandle2>), 200)]
        [ProducesResponseType(typeof(ApiError), 400)]
        public async Task<IActionResult> GetHistoryCandles(
            MarketType market,
            string assetPair,
            Period period,
            PriceType type,
            DateTime from,
            DateTime to)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToApiError());
            }

            var candlesService = _candlesServiceProvider.TryGet(market.ToDomain());

            if (candlesService == null)
            {
                return BadRequest(new ApiError
                {
                    Code = ErrorCodes.InvalidInput,
                    Msg = "Invalid market"
                });
            }

            CandlesHistoryResponseModel history;

            try
            {
                history = await candlesService.GetCandlesHistoryAsync(
                    assetPair,
                    type.ToCandlesHistoryServiceModel(),
                    period.ToCandlesHistoryServiceApiModel(),
                    from,
                    to);
            }
            catch (Lykke.Service.CandlesHistory.Client.Custom.ErrorResponseException e)
            {
                var errorMessage = e.Error.ErrorMessages != null && e.Error.ErrorMessages.Any()
                    ? string.Join(",", e.Error.ErrorMessages.Values.SelectMany(msg => msg).ToArray())
                    : "History for asset pair is not available";

                return BadRequest(new ApiError
                {
                    Code = ErrorCodes.InvalidInput,
                    Msg = errorMessage
                });
            }

            var response = new CandlesHistoryResponse<ApiCandle2>
            {
                AssetPair = assetPair,
                Period = period,
                DateFrom = from,
                DateTo = to,
                Type = type,
                Data = history.History.ToApiModel2().ToList()
            };

            return Ok(response);
        }
    }
}
