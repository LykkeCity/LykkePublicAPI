using Autofac;
using AzureRepositories.Accounts;
using AzureRepositories.Exchange;
using AzureRepositories.Feed;
using AzureStorage.Tables;
using Common.Log;
using Core.Domain.Accounts;
using Core.Domain.Exchange;
using Core.Domain.Feed;
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
            builder.Register(c => new AssetPairBestPriceRepository(AzureTableStorage<FeedDataEntity>.Create(
                    _dbSettings.ConnectionString(x => x.HLiquidityConnString),
                    "MarketProfile",
                    c.Resolve<ILog>())))
                .As<IAssetPairBestPriceRepository>();

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

            builder.Register(c => new WalletsRepository(AzureTableStorage<WalletEntity>.Create(
                    _dbSettings.ConnectionString(x => x.BalancesInfoConnString),
                    "Accounts",
                    c.Resolve<ILog>())))
                .As<IWalletsRepository>();
        }
    }
}
