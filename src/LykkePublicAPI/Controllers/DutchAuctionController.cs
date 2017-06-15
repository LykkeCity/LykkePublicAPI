using System.Linq;
using System.Threading.Tasks;
using Core.Domain.DutchAuction;
using Core.Services;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class DutchAuctionController : Controller
    {
        private readonly IDutchAuctionService _dutchAuctionService;

        public DutchAuctionController(IDutchAuctionService dutchAuctionService)
        {
            _dutchAuctionService = dutchAuctionService;
        }

        [HttpGet]
        [Route("orderbook")]
        public async Task<ApiDutchAuctionOrderbook> GetOrderBook()
        {
            var orderbook = await _dutchAuctionService.GetOrderBookAsync();

            return new ApiDutchAuctionOrderbook
            {
                Price = orderbook.Price,
                InMoneyVolume = orderbook.InMoneyVolume,
                OutOfTheMoneyVolume = orderbook.OutOfTheMoneyVolume,
                InMoneyOrders = orderbook.InMoneyOrders.Select(Map),
                OutOfTheMoneyOrders = orderbook.OutOfTheMoneyOrders.Select(Map)
            };
        }

        private ApiDutchAuctionOrderbook.Order Map(DutchAuctionOrder order)
        {
            return new ApiDutchAuctionOrderbook.Order
            {
                Price = order.Price,
                Investors = order.Investors,
                Volume = order.Volume
            };
        }
    }
}