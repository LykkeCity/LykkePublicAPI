using System.Threading.Tasks;
using Lykke.Service.Assets.Client.Custom;

namespace Core.Services
{
    public interface ISrvRatesHelper
    {
        Task<double> GetRate(string neededAssetId, IAssetPair assetPair);
    }
}
