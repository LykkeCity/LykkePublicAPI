﻿using System.Threading.Tasks;
using Common;
using Core.Services;
using Lykke.Service.Assets.Client.Custom;

namespace Services
{
    public class SrvRateHelper : ISrvRatesHelper
    {
        private readonly IMarketProfileService _marketProfileService;

        public SrvRateHelper(IMarketProfileService marketProfileService)
        {
            _marketProfileService = marketProfileService;
        }

        public async Task<double> GetRate(string neededAssetId, IAssetPair assetPair)
        {
            var rates = await _marketProfileService.TryGetPairAsync(assetPair.Id);

            return GetRate(neededAssetId, assetPair, rates.AskPrice);
        }

        private double GetRate(string neededAssetId, IAssetPair assetPair, double price)
        {
            var inverted = assetPair.IsInverted(neededAssetId);
            int accuracy = inverted ? assetPair.Accuracy : assetPair.InvertedAccuracy;
            var rate = inverted ? price : 1 / price;

            return rate.TruncateDecimalPlaces(accuracy);
        }
    }
}
