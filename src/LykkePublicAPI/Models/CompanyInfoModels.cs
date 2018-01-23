namespace LykkePublicAPI.Models
{
    public class CompanyInfoModels
    {
        public double TotalLykkeCoins { get; set; }
        public double PrivateWalletsCoins { get; set; }
        public double TradingWalletsCoins { get; set; }
        public double TreasuryCoins { get; set; }
    }

    public class RegistrationsCountModel
    {
        public long Count { get; set; }
    }
}
