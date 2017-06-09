using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.MarketProfileService.Client.Models;

namespace Core.Services
{
    public interface IMarketProfileService
    {
        Task<AssetPairModel> TryGetPairAsync(string assetPairId);
        Task<IList<AssetPairModel>> GetAllPairsAsync();
    }
}