using System;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Domain.Settings;
using Core.Services;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class CompanyController : Controller
    {
        private readonly LykkeCompanyData _companyInfo;
        private readonly IMarketCapitalizationService _marketCapitalizationService;
        private readonly IRegistrationsInfoCacheService _registrationsInfoCacheService;
        private readonly INinjaNetworkClient _ninjaService;

        public CompanyController(
            LykkeCompanyData companyInfo,
            IMarketCapitalizationService marketCapitalizationService,
            IRegistrationsInfoCacheService registrationsInfoCacheService,
            INinjaNetworkClient ninjaService
            )
        {
            _companyInfo = companyInfo;
            _marketCapitalizationService = marketCapitalizationService ?? throw new ArgumentNullException(nameof(marketCapitalizationService));
            _registrationsInfoCacheService = registrationsInfoCacheService ?? throw new ArgumentNullException(nameof(registrationsInfoCacheService));
            _ninjaService = ninjaService ?? throw new ArgumentNullException(nameof(ninjaService));
        }

        /// <summary>
        /// Get assets dictionary
        /// </summary>
        [HttpGet("ownershipStructure")]
        public async Task<CompanyInfoModels> GetOwnershipStructure()
        {
            var tradingWalletCoins = await _marketCapitalizationService.GetCapitalization(LykkeConstants.LykkeAssetId) ?? 0;

            var treasuryAmountTasks = _companyInfo.LkkTreasuryWallets.Select(address => _ninjaService.GetBalanceAsync(address, LykkeConstants.LykkeAssetId));
            var treasuryAmount = (await Task.WhenAll(treasuryAmountTasks)).Sum();

            var privateWalletCoins = _companyInfo.LkkTotalAmount - tradingWalletCoins -
                                     treasuryAmount;

            return new CompanyInfoModels
            {
                TotalLykkeCoins = _companyInfo.LkkTotalAmount,
                TradingWalletsCoins = tradingWalletCoins,
                PrivateWalletsCoins = privateWalletCoins,
                TreasuryCoins = treasuryAmount
            };
        }
        
        /// <summary>
        /// Get registrations count
        /// </summary>
        [HttpGet("registrationsCount")]
        public async Task<RegistrationsCountModel> GetRegistrationsCount()
        {
            return new RegistrationsCountModel
            {
                Count = await _registrationsInfoCacheService.GetRegistrationsCountAsync()
            };
        }
    }
}
