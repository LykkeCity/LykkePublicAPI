using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Assets;

namespace Core.Domain.Accounts
{

    public interface IWallet
    {
        double Balance { get; }
        string AssetId { get; }
    }

    public class Wallet : IWallet
    {
        public string AssetId { get; set; }
        public double Balance { get; set; }

        public static Wallet Create(IAsset asset, double balance = 0)
        {
            return new Wallet
            {
                AssetId = asset.Id,
                Balance = balance
            };
        }
    }

    public interface IWalletsRepository
    {
        Task<IEnumerable<IWallet>> GetAsync(string clientId);
        Task<IWallet> GetAsync(string clientId, string assetId);
        Task UpdateBalanceAsync(string clientId, string assetId, double balance);
        Task<Dictionary<string, double>> GetTotalBalancesAsync();

        Task GetWalletsByChunkAsync(Func<IEnumerable<KeyValuePair<string, IEnumerable<IWallet>>>, Task> chunk);
    }


    public static class WalletsRespostoryExtention
    {
        public static async Task<double> GetWalletBalanceAsync(this IWalletsRepository walletsRepository, string clientId, string assetId)
        {
            var entity = await walletsRepository.GetAsync(clientId, assetId);
            if (entity == null)
                return 0;

            return entity.Balance;
        }

        public static async Task<IEnumerable<IWallet>> GetAsync(this IWalletsRepository walletsRepository, string clientId,
            IEnumerable<IAsset> assets)
        {
            var wallets = await walletsRepository.GetAsync(clientId);


            return assets.Select(asset => wallets.FirstOrDefault(wallet => wallet.AssetId == asset.Id) ?? Wallet.Create(asset) );
        }
    }
}
