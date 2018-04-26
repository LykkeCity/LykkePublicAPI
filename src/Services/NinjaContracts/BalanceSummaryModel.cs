using Newtonsoft.Json;

namespace Services.NinjaContracts
{
    public class BalanceSummaryModel
    {
        public class SummaryData
        {
            [JsonProperty("transactionCount")]
            public int Count { get; set; }

            [JsonProperty("amount")]
            public double Amount { get; set; }

            [JsonProperty("received")]
            public double Recieved { get; set; }

            [JsonProperty("assets")]
            public ColoredSummaryData[] Assets { get; set; }
        }

        public class ColoredSummaryData
        {
            [JsonProperty("asset")]
            public string AssetId { get; set; }

            [JsonProperty("quantity")]
            public double Quantity { get; set; }

            [JsonProperty("received")]
            public double Recieved { get; set; }
        }

        [JsonProperty("unConfirmed")]
        public SummaryData Unconfirmed { get; set; }

        [JsonProperty("confirmed")]
        public SummaryData Confirmed { get; set; }

        [JsonProperty("spendable")]
        public SummaryData Spendable { get; set; }

        [JsonProperty("immature")]
        public SummaryData Immature { get; set; }
    }
}
