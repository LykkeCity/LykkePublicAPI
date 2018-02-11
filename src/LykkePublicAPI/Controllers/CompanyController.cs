using System.Threading.Tasks;
using Core;
using Core.Domain.Settings;
using Core.Services;
using Lykke.Service.Registration;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class CompanyController : Controller
    {
        private readonly LykkeCompanyData _companyInfo;
        private readonly IMarketCapitalizationService _marketCapitalizationService;
        private readonly ILykkeRegistrationClient _registrationClient;

        public CompanyController(
            LykkeCompanyData companyInfo,
            IMarketCapitalizationService marketCapitalizationService,
            ILykkeRegistrationClient registrationClient
            )
        {
            _companyInfo = companyInfo;
            _marketCapitalizationService = marketCapitalizationService;
            _registrationClient = registrationClient;
        }

        /// <summary>
        /// Get assets dictionary
        /// </summary>
        [HttpGet("ownershipStructure")]
        public async Task<CompanyInfoModels> GetOwnershipStructure()
        {
            var tradingWalletCoins = await _marketCapitalizationService.GetCapitalization(LykkeConstants.LykkeAssetId);
            var privateWalletCoins = _companyInfo.LkkTotalAmount - tradingWalletCoins -
                                     _companyInfo.LkkCompanyTreasuryAmount;

            return new CompanyInfoModels
            {
                TotalLykkeCoins = _companyInfo.LkkTotalAmount,
                TradingWalletsCoins = tradingWalletCoins,
                PrivateWalletsCoins = privateWalletCoins,
                TreasuryCoins = _companyInfo.LkkCompanyTreasuryAmount
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
                Count = await _registrationClient.GetRegistrationsCountAsync()
            };
        }
    }
}
