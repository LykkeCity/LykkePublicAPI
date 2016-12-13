using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Domain.Exchange
{
    public interface IMarketData
    {
        string AssetPairId { get; set; }
        double UsdVolume { get; set; }
        double LastPriceUsd { get; set; }
        DateTime Dt { get; set; }
    }

    public class MarketData : IMarketData
    {
        public string AssetPairId { get; set; }
        public double UsdVolume { get; set; }
        public double LastPriceUsd { get; set; }
        public DateTime Dt { get; set; }
    }

    public interface IMarketDataRepository
    {
        Task<IEnumerable<IMarketData>> Get24HMarketsAsync();
    }
}
