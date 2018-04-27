using System.Threading.Tasks;

namespace Core.Services
{
    public interface INinjaNetworkClient
    {
        /// <summary>
        /// Gets colored balance for the wallet address.
        /// </summary>
        /// <param name="address">The address of the wallet.</param>
        /// <param name="assetName">The name of target asset.</param>
        /// <returns>The summary colored balance for the given asset OR 0 if the asset does not exist.</returns>
        /// <exception cref="ArgumentNullException">When <see cref="address"/> or <see cref="assetName"/> is null or empty string.</exception>
        /// <exception cref="IOException">When asset service is unavailable or returns nothing.</exception>
        Task<double> GetBalanceAsync(string address, string assetName);
    }
}
