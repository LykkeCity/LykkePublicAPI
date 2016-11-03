using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Domain.Assets;
using Core.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Assets
{
    public class AssetEntity : TableEntity, IAsset
    {
        public static string GeneratePartitionKey()
        {
            return "Asset";
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public string Id => RowKey;
        public string BlockChainId { get; set; }
        public string BlockChainAssetId { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string IdIssuer { get; set; }
        public bool IsBase { get; set; }
        public bool HideIfZero { get; set; }
        public int Accuracy { get; set; }
        public double Multiplier { get; set; }
        public bool IsDisabled { get; set; }
        public bool HideWithdraw { get; set; }
        public bool HideDeposit { get; set; }
        public int DefaultOrder { get; set; }
        public bool KycNeeded { get; set; }
        public string AssetAddress { get; set; }
        public bool BankCardsDepositEnabled { get; set; }
        public bool SwiftDepositEnabled { get; set; }
        public bool BlockchainDepositEnabled { get; set; }
        public double DustLimit { get; set; }
        public string CategoryId { get; set; }

        public static AssetEntity Create(IAsset asset)
        {
            return new AssetEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(asset.Id),
                BlockChainId = asset.BlockChainId,
                Name = asset.Name,
                IsBase = asset.IsBase,
                Symbol = asset.Symbol,
                IdIssuer = asset.IdIssuer,
                HideIfZero = asset.HideIfZero,
                BlockChainAssetId = asset.BlockChainAssetId,
                Accuracy = asset.Accuracy,
                Multiplier = asset.Multiplier,
                IsDisabled = asset.IsDisabled,
                HideDeposit = asset.HideDeposit,
                HideWithdraw = asset.HideWithdraw,
                DefaultOrder = asset.DefaultOrder,
                KycNeeded = asset.KycNeeded,
                AssetAddress = asset.AssetAddress,
                BankCardsDepositEnabled = asset.BankCardsDepositEnabled,
                SwiftDepositEnabled = asset.SwiftDepositEnabled,
                BlockchainDepositEnabled = asset.BlockchainDepositEnabled,
                DustLimit = asset.DustLimit,
                CategoryId = asset.CategoryId
            };
        }
    }

    public class AssetsRepository : IAssetsRepository
    {
        private readonly INoSQLTableStorage<AssetEntity> _tableStorage;

        public AssetsRepository(INoSQLTableStorage<AssetEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task RegisterAssetAsync(IAsset asset)
        {
            var newAsset = AssetEntity.Create(asset);
            return _tableStorage.InsertAsync(newAsset);
        }

        public async Task EditAssetAsync(string id, IAsset asset)
        {
            await _tableStorage.DeleteAsync(AssetEntity.GeneratePartitionKey(), AssetEntity.GenerateRowKey(id));
            await RegisterAssetAsync(asset);
        }


        public async Task<IEnumerable<IAsset>> GetAssetsAsync()
        {
            var partitionKey = AssetEntity.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(partitionKey);
        }

        public async Task<IAsset> GetAssetAsync(string id)
        {
            var partitionKey = AssetEntity.GeneratePartitionKey();
            var rowKey = AssetEntity.GenerateRowKey(id);

            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public async Task<IEnumerable<IAsset>> GetAssetsForCategoryAsync(string category)
        {
            var partitionKey = AssetEntity.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(partitionKey, x => x.CategoryId == category);
        }

        public async Task SetDisabled(string id, bool value)
        {
            await _tableStorage.ReplaceAsync(AssetEntity.GeneratePartitionKey(), AssetEntity.GenerateRowKey(id),
                assetEntity =>
                {
                    assetEntity.IsDisabled = value;
                    return assetEntity;
                });
        }
    }
}