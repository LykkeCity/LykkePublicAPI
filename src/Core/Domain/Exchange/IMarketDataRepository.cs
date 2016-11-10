using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Domain.Exchange
{
    public interface IMarketData
    {
        string AssetPairId { get; set; }
        double LkkVolume { get; set; }
        double LastPrice { get; set; }
        DateTime Dt { get; set; }
    }

    public class MarketData : IMarketData
    {
        public string AssetPairId { get; set; }
        public double LkkVolume { get; set; }
        public double LastPrice { get; set; }
        public DateTime Dt { get; set; }
    }

    public interface IMarketDataRepository
    {
        Task AddOrReplaceMarketData(IEnumerable<IMarketData> data);
        Task<IEnumerable<IMarketData>> Get24HMarketsAsync();
    }
}
