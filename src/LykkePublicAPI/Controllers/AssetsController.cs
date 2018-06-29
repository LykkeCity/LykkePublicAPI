using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Assets.Client;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class AssetsController
    {
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;

        public AssetsController(IAssetsServiceWithCache assetsServiceWithCache)
        {
            _assetsServiceWithCache = assetsServiceWithCache;
        }

        /// <summary>
        /// Get assets dictionary
        /// </summary>
        [HttpGet("dictionary")]
        public async Task<IEnumerable<ApiAsset>> GetDictionary()
        {
            var assets = (await _assetsServiceWithCache.GetAllAssetsAsync(false)).Where(x => !x.IsDisabled);

            return assets.ToApiModel();
        }
    }
}
