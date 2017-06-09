using System.Threading.Tasks;
using Core.Domain.DutchAuction;
using Core.Services;
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
        public async Task<DutchAuctionOrderBookItem[]> GetOrderBook()
        {
            return await _dutchAuctionService.GetOrderBookAsync();
        }
    }
}