using System.Threading.Tasks;
using Core.Domain.Assets;

namespace Core.Services
{
    public interface ISrvRatesHelper
    {
        Task<double> GetRate(string neededAssetId, IAssetPair assetPair);
        double GetRate(string neededAssetId, IAssetPair assetPair, double price);
    }
}
