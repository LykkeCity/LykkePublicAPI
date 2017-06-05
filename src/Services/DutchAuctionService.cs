using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Core.Domain.DutchAuction;
using Core.Services;

namespace Services
{
    public class DutchAuctionService : 
        IDutchAuctionService,
        IDisposable
    {
        private readonly HttpClient _client;

        public DutchAuctionService(Uri dutchActionServiceUrl)
        {
            _client = new HttpClient
            {
                BaseAddress = dutchActionServiceUrl
            };

            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Add("User-Agent", "Lykke.PublicAPI");
        }

        public async Task<DutchAuctionOrderBook> GetOrderBookAsync()
        {
            var content = await _client.GetStringAsync("/api/orderbook");

            return Newtonsoft.Json.JsonConvert.DeserializeObject<DutchAuctionOrderBook>(content);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}