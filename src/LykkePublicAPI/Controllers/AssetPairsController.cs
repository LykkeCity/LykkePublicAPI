using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Core.Domain.Assets;
using Core.Domain.Feed;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]/rate")]
    public class AssetPairsController : Controller
    {
        private readonly IAssetPairBestPriceRepository _assetPairBestPriceRepository;

        public AssetPairsController(IAssetPairBestPriceRepository assetPairBestPriceRepository)
        {
            _assetPairBestPriceRepository = assetPairBestPriceRepository;
        }

        /// <summary>
        /// Get all asset pair rates
        /// </summary>
        [HttpGet]
        public async Task<IEnumerable<ApiAssetPairRateModel>> Get()
        {
            var marketProfile = await _assetPairBestPriceRepository.GetAsync();
            return marketProfile.ToApiModel();
        }

        /// <summary>
        /// Get rates for asset pair
        /// </summary>
        [HttpGet("{assetPairId}")]
        public async Task<ApiAssetPairRateModel> Get(string assetPairId)
        {
            return (await _assetPairBestPriceRepository.GetAsync(assetPairId))?.ToApiModel();
        }
    }
}
