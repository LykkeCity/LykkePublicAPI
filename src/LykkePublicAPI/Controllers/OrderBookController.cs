using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Domain.OrderBook;
using Core.Services;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        public Task<IEnumerable<IOrderBook>> Get()
        {
            return _orderBooksService.GetAllAsync();
        }

        [HttpGet("{assetPairId}")]
        public Task<IEnumerable<IOrderBook>> Get(string assetPairId, [FromQuery]bool isBuy)
        {
            return _orderBooksService.GetAsync(assetPairId);
        }
    }
}
