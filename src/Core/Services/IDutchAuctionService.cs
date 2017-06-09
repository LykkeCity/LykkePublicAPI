using System.Threading.Tasks;
using Core.Domain.DutchAuction;

namespace Core.Services
{
    public interface IDutchAuctionService
    {
        Task<DutchAuctionOrderBookItem[]> GetOrderBookAsync();
    }
}