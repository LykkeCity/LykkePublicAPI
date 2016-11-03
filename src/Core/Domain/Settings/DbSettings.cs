using System.Linq;
using System.Net;

namespace Core.Domain.Settings
{
    public class IpEndpointSettings
    {
        public string InternalHost { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        public IPEndPoint GetClientIpEndPoint(bool useInternal = false)
        {
            return new IPEndPoint(IPAddress.Parse(useInternal ? InternalHost : Host), Port);
        }

        public IPEndPoint GetServerIpEndPoint()
        {
            return new IPEndPoint(IPAddress.Any, Port);
        }

    }

    public class DbSettings
    {
        public string ClientPersonalInfoConnString { get; set; }
        public string BalancesInfoConnString { get; set; }
        public string ALimitOrdersConnString { get; set; }
        public string HLimitOrdersConnString { get; set; }
        public string HMarketOrdersConnString { get; set; }
        public string HTradesConnString { get; set; }
        public string HLiquidityConnString { get; set; }
        public string BackOfficeConnString { get; set; }
        public string BitCoinQueueConnectionString { get; set; }
        public string DictsConnString { get; set; }
        public string LogsConnString { get; set; }
        public string SharedStorageConnString { get; set; }
        public string OlapConnString { get; set; }
        public string OlapLogsConnString { get; set; }
    }


    public class MatchingOrdersSettings
    {
        public IpEndpointSettings IpEndpoint { get; set; }
    }

    public class JobsSettings
    {
        public string NotificationsHubName { get; set; }
        public string NotificationsHubConnectionString { get; set; }
        public int DefaultConfirmationsLimit { get; set; }
        public int OrdinaryCashOutConfirmationsLimit { get; set; }
        public int CashInConfirmationsLimit { get; set; }
        public int TransferConfirmationsLimit { get; set; }
        public bool IsDebug { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpLogin { get; set; }
        public string SmtpPwd { get; set; }
        public string EmailFrom { get; set; }
        public string EmailFromDisplayName { get; set; }
        public string NexmoAppKey { get; set; }
        public string NexmoAppSecret { get; set; }
        public string DefaultSmsSender { get; set; }
        public string USCanadaSmsSender { get; set; }
        public bool EmailTemplatesRemote { get; set; }
        public string EmailTemplateHost { get; set; }
        public int RefundTimeoutInMinutes { get; set; }
        public bool IsDevEnv { get; set; }
        public int TxDetectorConfirmationsLimit { get; set; }
        public string MarketMakerId { get; set; }

        public int RefundTimeOutInDays => RefundTimeoutInMinutes / 60 / 24;
    }

    public class WalletBackendServices
    {
        public string CreateUnsignedTransferPath { get; set; }
        public string SignTransactionIfRequiredAndBroadcastPath { get; set; }
        public string SignTransactionPath { get; set; }
        public string GetWalletPath { get; set; }
        public string IsTransactionFullyIndexedUrlFormat { get; set; }
    }

    /// <summary>
    /// system parameters for Credit voucers API. See https://www.creditvouchers.com/site/getapi.html
    /// </summary>
    public class CreditVouchersSettings
    {
        public string ApiUserName { get; set; }

        public string ParthnerEmail { get; set; }

        public string ApiVersion { get; set; }

        public string AggregatorPaymentUrl { get; set; }

        public string MockUrl { get; set; }

        public string ApiPassword { get; set; }

        public string ApiKey { get; set; }

        public string Action { get; set; }

        public string PaymentType { get; set; }

        public string PaymentOkUrlFormat { get; set; }

        public string PaymentFailUrlFormat { get; set; }

        public string PaymentNotifyUrlFormat { get; set; }

        /// <summary>
        /// HardCoded asset used for Credit Vouchers processing
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// Wallet of this client is used for cash out for cash in operations for others
        /// </summary>
        public string SourceClientId { get; set; }

        /// <summary>
        /// If content of aggregator page matches this regex - client have to get PaymentUrl in BankCardPaymentUrl again
        /// </summary>
        public string ReloadRegex { get; set; }

        public double MinAmount { get; set; }

        public double MaxAmount { get; set; }
    }

    public class PaymentSystemsSettings
    {
        public CreditVouchersSettings CreditVouchers { get; set; }
    }

    public class ExternalLinksSettings
    {
        public string TermsOfUse { get; set; }
        public string InformationBrochure { get; set; }
        public string RefundInfo { get; set; }
        public string SupportPhoneNum { get; set; }
        public string UserAgreementUrl { get; set; }
    }

    public class SwiftCredentialsSettings
    {
        public string BIC { get; set; }
        public string AccountNumberCHF { get; set; }
        public string AccountNumberUSD { get; set; }
        public string AccountNumberEUR { get; set; }
        public string AccountNumberGBP { get; set; }
        public string AccountName { get; set; }
        public string PurposeOfPayment { get; set; }
        public string BankAddress { get; set; }
        public string CompanyAddress { get; set; }
    }

    public static class SwiftCredentialsSettingsHelper
    {
        public static string GetAccountNumber(this SwiftCredentialsSettings settings, string assetId)
        {
            switch (assetId)
            {
                case LykkeConstants.ChfAssetId:
                    return settings.AccountNumberCHF;
                case LykkeConstants.UsdAssetId:
                    return settings.AccountNumberUSD;
                case LykkeConstants.EurAssetId:
                    return settings.AccountNumberEUR;
                case LykkeConstants.GbpAssetId:
                    return settings.AccountNumberGBP;
            }

            return null;
        }
    }

    public class LykkeNewsSettings
    {
        public string RssUrl { get; set; }
        public string RssUrlWithMarkup { get; set; }
        public string RegexToGrabFirstImg { get; set; }
    }

    public class EtheriumSettings
    {
        public string ClientRegisterUrl { get; set; }
        public string QueueConnectionString { get; set; }
    }

    public class ExchangeSettings
    {
        public double MinBtcOrderAmount { get; set; }
    }

    public static class ChannelTypes
    {
        public const string Errors = "Errors";
        public const string Warnings = "Warnings";
    }

    public class SlackIntegrationSettings
    {
        public class Channel
        {
            public string Type { get; set; }
            public string WebHookUrl { get; set; }
        }

        public string Env { get; set; }
        public Channel[] Channels { get; set; }
    }

    public static class SlackIntegrationSettingsExt
    {
        public static string GetChannelWebHook(this SlackIntegrationSettings settings, string type)
        {
            return settings.Channels.FirstOrDefault(x => x.Type == type)?.WebHookUrl;
        }
    }

    public class TwilioSettings
    {
        public string AccountSid { get; set; }
        public string AuthToken { get; set; }
        public string SwissNumber { get; set; }
        public string UsNumber { get; set; }
    }

    public class LykkeServiceApiSettings
    {
        public string ServiceUri { get; set; }
    }

    public class ServicesMonitoringSettings
    {
        public class HostToCheck
        {
            public string ServiceName { get; set; }
            public string Url { get; set; }
        }

        public HostToCheck[] HostsToCheck { get; set; }
    }

    public class SolarCoinSettings
    {
        public string GetAddressUrl { get; set; }
        public string QueueConnectionString { get; set; }
    }

    public class BaseSettings
    {
        public DbSettings Db { get; set; }
        public MatchingOrdersSettings MatchingEngine { get; set; }

        public PaymentSystemsSettings PaymentSystems { get; set; }

        public ExternalLinksSettings ExternalLinks { get; set; }

        public SwiftCredentialsSettings SwiftCredentials { get; set; }

        public LykkeNewsSettings LykkeNews { get; set; }

        public EtheriumSettings Ethereum { get; set; }

        public ExchangeSettings Exchange { get; set; }

        public LykkeServiceApiSettings LykkeServiceApi { get; set; }

        public SlackIntegrationSettings SlackIntegration { get; set; }

        public TwilioSettings Twilio { get; set; }

        public ServicesMonitoringSettings ServicesMonitoring { get; set; }

        public SolarCoinSettings SolarCoin { get; set; }

        public string BlockChainExplorerUrl { get; set; }
        public string NinjaUrl { get; set; }
        public bool IsProduction { get; set; }

        public JobsSettings Jobs { get; set; }
        public WalletBackendServices WalletBackendServices { get; set; }

        public NetworkType NetworkType { get; set; }
        public bool UsePushPrivateKeyService { get; set; }
        //ToDo: ServiceBus relay. To remove
        public string PrivateKeyServicePushMethodUrl { get; set; }
        //WalletBackend endpoint
        public string PrivateKeyServicePushMethodPath { get; set; }

        public string GetReferralCodeByIpServicePath { get; set; }

        public int BackupWarningTimeoutMinutes { get; set; }
    }
}
