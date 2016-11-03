using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Assets;
using Core.Domain.Reports;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class TradeVolumeController : Controller
    {
        private readonly ITradeVolumesRepository _tradeVolumesRepository;
        private readonly IAssetsRepository _assetsRepository;

        public TradeVolumeController(ITradeVolumesRepository tradeVolumesRepository,
            IAssetsRepository assetsRepository)
        {
            _tradeVolumesRepository = tradeVolumesRepository;
            _assetsRepository = assetsRepository;
        }

        /// <summary>
        /// Get trade volumes for all assets
        /// </summary>
        [HttpGet]
        public async Task<IEnumerable<ApiTradeVolume>> Get()
        {
            var assets = await _assetsRepository.GetAssetsAsync();
            var ids = assets.Where(x => !x.IsDisabled).Select(x => x.Id).ToArray();

            return (await _tradeVolumesRepository.GetLastRecords(ids))?.ToApiModel();
        }

        /// <summary>
        /// Get trade volume for asset
        /// </summary>
        [HttpGet("{assetId}")]
        public async Task<ApiTradeVolume> Get(string assetId)
        {
            return (await _tradeVolumesRepository.GetLastRecord(assetId))?.ToApiModel();
        }
    }
}
