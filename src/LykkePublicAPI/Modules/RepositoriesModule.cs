using Autofac;
using AzureRepositories.Exchange;
using AzureRepositories.Feed;
using AzureStorage.Tables;
using Common.Log;
using Core.Domain.Exchange;
using Core.Domain.Settings;
using Core.Feed;
using Lykke.SettingsReader;

namespace LykkePublicAPI.Modules
{
    public class RepositoriesModule : Module
    {
        private readonly IReloadingManager<DbSettings> _dbSettings;

        public RepositoriesModule(IReloadingManager<DbSettings> dbSettings)
        {
            _dbSettings = dbSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new TradesCommonRepository(AzureTableStorage<TradeCommonEntity>.Create(
                    _dbSettings.ConnectionString(x => x.HTradesConnString),
                    "TradesCommon",
                    c.Resolve<ILog>())))
                .As<ITradesCommonRepository>();

            builder.Register(c => new FeedHistoryRepository(AzureTableStorage<FeedHistoryEntity>.Create(
                    _dbSettings.ConnectionString(x => x.HLiquidityConnString),
                    "FeedHistory",
                    c.Resolve<ILog>())))
                .As<IFeedHistoryRepository>();
        }
    }
}
