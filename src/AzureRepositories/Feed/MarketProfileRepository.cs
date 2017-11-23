using System;
using System.Threading.Tasks;
using Core.Domain.Feed;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;
using AzureStorage;

namespace AzureRepositories.Feed
{

    public class FeedDataEntity : TableEntity, IFeedData
    {
        public static class Profile
        {
            public static string GeneratePartitionKey()
            {
                return "Feed";
            }

            public static string GenerateRowKey(string asset)
            {
                return asset;
            }

            public static FeedDataEntity CreateNew(IFeedData feedData)
            {
                return new FeedDataEntity
                {
                    PartitionKey = GeneratePartitionKey(),
                    RowKey = GenerateRowKey(feedData.Asset),
                    DateTime = feedData.DateTime,
                    Bid = feedData.Bid,
                    Ask = feedData.Ask
                };
            }

        }


        public string Asset => RowKey;
        public DateTime DateTime { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }



    }

    public class AssetPairBestPriceRepository : IAssetPairBestPriceRepository
    {
        private readonly INoSQLTableStorage<FeedDataEntity> _tableStorage;

        public AssetPairBestPriceRepository(INoSQLTableStorage<FeedDataEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<MarketProfile> GetAsync()
        {
            var result = await
                _tableStorage.GetDataAsync();

            var profilePartitionKey = FeedDataEntity.Profile.GeneratePartitionKey();

            return new MarketProfile
            {
                Profile = result.Where(itm => itm.PartitionKey == profilePartitionKey).ToArray()
            };

        }

        public async Task<IFeedData> GetAsync(string assetPairId)
        {
            var partitionKey = FeedDataEntity.Profile.GeneratePartitionKey();
            var rowKey = FeedDataEntity.Profile.GenerateRowKey(assetPairId);

            return await _tableStorage.GetDataAsync(partitionKey, rowKey);

        }

        public async Task SaveAsync(IFeedData feedData)
        {
            var newEntity = FeedDataEntity.Profile.CreateNew(feedData);
            await _tableStorage.InsertOrReplaceAsync(newEntity);
        }
    }
}
