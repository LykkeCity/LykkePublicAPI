using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Domain.Assets
{
    public interface IAsset
    {
        string Id { get; }
        string BlockChainId { get; }
        string BlockChainAssetId { get; }
        string Name { get; }
        string Symbol { get; }
        string IdIssuer { get; }
        bool IsBase { get; }
        bool HideIfZero { get; }
        int Accuracy { get; }
        double Multiplier { get; }
        bool IsDisabled { get; }
        bool HideWithdraw { get; }
        bool HideDeposit { get; }
        int DefaultOrder { get; }
        bool KycNeeded { get; }
        string AssetAddress { get; }
        bool BankCardsDepositEnabled { get; }
        bool SwiftDepositEnabled { get; }
        bool BlockchainDepositEnabled { get; }
        double DustLimit { get; }
        string CategoryId { get; }
        string DisplayId { get; set; }
    }

    public class Asset : IAsset
    {
        public string Id { get; set; }
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
        public string DisplayId { get; set; }

        public static Asset Create(string id, string blockChainId, string categoryId, string name, string symbol, string idIssuer, bool hideIfZero,
            bool bankCardsDeposit, bool swiftEnabled, bool blockChainDepositEnabled, bool isDisabled = false, bool hideWithdraw = false, bool hideDeposit = false,
            int defaultOrder = 999, bool kycNeeded = false)
        {
            return new Asset
            {
                Id = id,
                Name = name,
                Symbol = symbol,
                IdIssuer = idIssuer,
                BlockChainId = blockChainId,
                HideIfZero = hideIfZero,
                Accuracy = 2,
                Multiplier = 1,
                IsDisabled = isDisabled,
                HideDeposit = hideDeposit,
                HideWithdraw = hideWithdraw,
                DefaultOrder = defaultOrder,
                KycNeeded = kycNeeded,
                BankCardsDepositEnabled = bankCardsDeposit,
                SwiftDepositEnabled = swiftEnabled,
                BlockchainDepositEnabled = blockChainDepositEnabled,
                CategoryId = categoryId
            };
        }

        public static Asset CreateDefault()
        {
            return new Asset();
        }
    }

    public interface IAssetsRepository
    {
        Task RegisterAssetAsync(IAsset asset);
        Task EditAssetAsync(string id, IAsset asset);
        Task<IEnumerable<IAsset>> GetAssetsAsync();
        Task<IAsset> GetAssetAsync(string id);
        Task<IEnumerable<IAsset>> GetAssetsForCategoryAsync(string category);
        Task SetDisabled(string id, bool value);
    }
}
