using System.Threading.Tasks;

namespace Core.Services
{
    public interface IRegistrationsInfoCacheService
    {
        Task<long> GetRegistrationsCountAsync();
    }
}
