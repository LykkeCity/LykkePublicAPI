using System.Threading.Tasks;

namespace Core.Services
{
    public interface IMarketCapitalizationService
    {
        Task<double?> GetCapitalization(string market);
    }
}
