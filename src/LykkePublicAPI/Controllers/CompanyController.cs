using System;
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

        public CompanyController(
            LykkeCompanyData companyInfo,
            IMarketCapitalizationService marketCapitalizationService,
            IRegistrationsInfoCacheService registrationsInfoCacheService
            )
        {
            _companyInfo = companyInfo;
            _marketCapitalizationService = marketCapitalizationService ?? throw new ArgumentNullException(nameof(marketCapitalizationService));
            _registrationsInfoCacheService = registrationsInfoCacheService ?? throw new ArgumentNullException(nameof(registrationsInfoCacheService));
        }

        /// <summary>
        /// Get assets dictionary
        /// </summary>
        [HttpGet("ownershipStructure")]
        public async Task<CompanyInfoModels> GetOwnershipStructure()
        {
            var tradingWalletCoins = await _marketCapitalizationService.GetCapitalization(LykkeConstants.LykkeAssetId) ?? 0;
            
            // since we removed ninja dependency, we can't calculate treasury amount
            var treasuryAmount = 0;

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
