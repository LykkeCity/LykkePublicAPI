using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core;
using Core.Services;
using Lykke.Service.Assets.Client.Custom;
using Services.NinjaContracts;

namespace Services
{
    /// <summary>
    /// The class responsible for communication with Bitcoin Ninja server.
    /// </summary>
    public class SrvNinjaHelper : ISrvNinjaHelper
    {
        private readonly ICachedAssetsService _assetsService;
        private readonly string _url;

        #region Initialization

        public SrvNinjaHelper(ICachedAssetsService assetsService, string url)
        {
            _assetsService = assetsService ?? throw new ArgumentNullException(nameof(assetsService));

            _url = 
                !string.IsNullOrWhiteSpace(url)
                ? url
                : throw new ArgumentNullException(nameof(url));
        }

        #endregion

        #region Public

        /// <inheritdoc cref="ISrvNinjaHelper"/>
        public async Task<double> GetBalance(string address, string assetName)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrWhiteSpace(assetName))
                throw new ArgumentNullException(nameof(assetName));

            var assets = await _assetsService.GetAllAssetsAsync();
            if (!assets?.Any() ?? false)
                throw new IOException("Could not obtain the list of available assets.");

            var asset = assets.FirstOrDefault(a => a.Id == assetName);
            if (asset == null)
                return 0; // There is no such an asset -> we have zero balance for it.

            var btc = assets.First(a => a.Id == LykkeConstants.BitcoinAssetId);

            var response = await ExecuteRequest<BalanceSummaryModel>($"{_url}balances/{address}/summary?colored=true");

            // Check if they asked for BTC balance. If so, we can go by a short circuit.
            if (asset.BlockChainAssetId == null)
                return response.Spendable.Amount * btc.Multiplier();

            var balanceSummaryForAsset =
                response.Spendable.Assets.FirstOrDefault(a => a.AssetId == asset.BlockChainAssetId);

            return (balanceSummaryForAsset?.Quantity ?? 0) * asset.Multiplier();
        }

        #endregion

        #region Private

        // May throw exception in case of network problems or data corruption (when unable to decerialize).
        // The possible exception should be handled in the caller code.
        private static async Task<T> ExecuteRequest<T>(string request)
        {
            var webRequest = (HttpWebRequest) WebRequest.Create(request);
            webRequest.Method = "GET";
            webRequest.ContentType = "application/x-www-form-urlencoded";

            var webResponse = await webRequest.GetResponseAsync();
            string resultString;

            using (var recieveStream = webResponse.GetResponseStream())
            {
                using (var sr = new StreamReader(recieveStream))
                {
                    resultString = await sr.ReadToEndAsync();
                }
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(resultString);
        }

        #endregion
    }
}
