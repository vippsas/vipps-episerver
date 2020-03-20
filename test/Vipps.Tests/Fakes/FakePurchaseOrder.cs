using System;
using System.Collections.Generic;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Internal;
using Mediachase.Commerce;

namespace Vipps.Test.Fakes
{
    public class FakePurchaseOrder : InMemoryOrderGroup, IPurchaseOrder
    {
        public FakePurchaseOrder(IOrderGroup orderGroup) : base(orderGroup)
        {
        }

        public FakePurchaseOrder(IMarket market, Currency currency) : base(market, currency)
        {
        }

        public string OrderNumber { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public ICollection<IReturnOrderForm> ReturnForms { get; }
    }
}
