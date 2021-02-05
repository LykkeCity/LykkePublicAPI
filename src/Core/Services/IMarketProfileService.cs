using System.Collections.Generic;
using System.Threading.Tasks;
using Antares.Service.MarketProfile.LykkeClient.Models;

namespace Core.Services
{
    public interface IMarketProfileService
    {
        Task<AssetPairModel> TryGetPairAsync(string assetPairId);
        Task<IEnumerable<AssetPairModel>> GetAllPairsAsync();
    }
}
