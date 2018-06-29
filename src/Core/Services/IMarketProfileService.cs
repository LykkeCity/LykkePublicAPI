using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.MarketProfile.Client.Models;

namespace Core.Services
{
    public interface IMarketProfileService
    {
        Task<AssetPairModel> TryGetPairAsync(string assetPairId);
        Task<IEnumerable<AssetPairModel>> GetAllPairsAsync();
    }
}
