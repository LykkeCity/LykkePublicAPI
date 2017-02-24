using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices.Contracts;

namespace Core.Services
{
    public interface IOrderBooksService
    {
        Task<IEnumerable<IOrderBook>> GetAllAsync();
        Task<IEnumerable<IOrderBook>> GetAsync(string assetPairId);
    }
}
