using System.Threading.Tasks;
using Lykke.Service.Assets.Client.Models;

namespace Core.Services
{
    public interface ISrvRatesHelper
    {
        Task<double> GetRate(string neededAssetId, AssetPair assetPair);
    }
}
