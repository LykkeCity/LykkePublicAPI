using System.Collections.Generic;

namespace Core.Domain.DutchAuction
{
    public class DutchAuctionOrderBook
    {
        public double Price { get; set; }
        public double InMoneyVolume { get; set; }
        public double OutOfTheMoneyVolume { get; set; }
        public IEnumerable<DutchAuctionOrder> InMoneyOrders { get; set; }
        public IEnumerable<DutchAuctionOrder> OutOfTheMoneyOrders { get; set; }
    }
}