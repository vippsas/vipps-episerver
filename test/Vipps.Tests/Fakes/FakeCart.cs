using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Internal;
using Mediachase.Commerce;

namespace Vipps.Test.Fakes
{
    public class FakeCart : InMemoryOrderGroup, ICart
    {
        public FakeCart(IOrderGroup orderGroup) : base(orderGroup)
        {
        }

        public FakeCart(IMarket market, Currency currency) : base(market, currency)
        {
        }
    }
}
