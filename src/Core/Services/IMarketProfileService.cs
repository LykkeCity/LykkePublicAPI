using System.Threading.Tasks;
using Core.Domain.Feed;

namespace Core.Services
{
    public interface IMarketProfileService
    {
        Task<MarketProfile> GetMarketProfileAsync();
        Task<IFeedData> GetFeedDataAsync(string assetPairId);
    }
}
