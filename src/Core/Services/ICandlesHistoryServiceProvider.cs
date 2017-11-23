using Core.Domain.Market;
using Lykke.Service.CandlesHistory.Client;

namespace Core.Services
{
    public interface ICandlesHistoryServiceProvider
    {
        ICandleshistoryservice TryGet(MarketType market);
        ICandleshistoryservice Get(MarketType market);
    }
}
