using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Core.Domain.Assets;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class AssetsController
    {
        private readonly CachedDataDictionary<string, IAsset> _assetsDict;

        public AssetsController(CachedDataDictionary<string, IAsset> assetsDict)
        {
            _assetsDict = assetsDict;
        }

        /// <summary>
        /// Get assets dictionary
        /// </summary>
        [HttpGet("dictionary")]
        public async Task<IEnumerable<ApiAsset>> GetDictionary()
        {
            var assets = (await _assetsDict.Values()).Where(x => !x.IsDisabled);

            return assets.ToApiModel();
        }
    }
}
