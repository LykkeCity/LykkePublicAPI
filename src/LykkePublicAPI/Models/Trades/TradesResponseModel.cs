using System;
using Lykke.Service.TradesAdapter.AutorestClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LykkePublicAPI.Models.Trades
{
    public class TradesResponseModel
    {
        public string Id { set; get; }
        public string AssetPairId { set; get; }
        public DateTime DateTime { set; get; }
        public double Volume { set; get; }
        public double Price { set; get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public TradeAction Action { set; get; }
    }

    public static class TradesToResponseModelConverter
    {
        public static TradesResponseModel ToResponseModel(this Trade trade)
        {
            return new TradesResponseModel
            {
                Id = trade.Id,
                AssetPairId = trade.AssetPairId,
                DateTime = trade.DateTime,
                Volume = trade.Volume,
                Price = trade.Price,
                Action = trade.Action
            };
        }
    }
}
