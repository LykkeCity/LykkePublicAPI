using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Services;
using Microsoft.AspNetCore.Mvc;
using Lykke.Domain.Prices.Contracts;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class OrderBookController : Controller
    {
        private readonly IOrderBooksService _orderBooksService;

        public OrderBookController(IOrderBooksService orderBooksService)
        {
            _orderBooksService = orderBooksService;
        }

        /// <summary>
        /// Get all orderbooks
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public Task<IEnumerable<IOrderBook>> Get()
        {
            return _orderBooksService.GetAllAsync();
        }

        /// <summary>
        /// Get orderbook for specified asset pair
        /// </summary>
        /// <param name="assetPairId"></param>
        /// <returns></returns>
        [HttpGet("{assetPairId}")]
        public Task<IEnumerable<IOrderBook>> Get(string assetPairId)
        {
            return _orderBooksService.GetAsync(assetPairId);
        }
    }
}
