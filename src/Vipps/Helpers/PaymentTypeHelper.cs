using System;
using EPiServer.Commerce.Order;
using Vipps.Models;

namespace Vipps.Helpers
{
    public static class PaymentTypeHelper
    {
        public static VippsPaymentType GetVippsPaymentType(ICart cart)
        {
            if (cart == null)
            {
                return VippsPaymentType.UNKNOWN;
            }

            if (Enum.TryParse<VippsPaymentType>(cart?.Properties[VippsConstants.VippsPaymentTypeField]?.ToString(),
                out var paymentType))
            {
                return paymentType;
            }

            return VippsPaymentType.CHECKOUT;
        }
        
    }
}