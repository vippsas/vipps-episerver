using System;
using EPiServer.Commerce.Order;
using Vipps.Models;

namespace Vipps.Helpers
{
    public static class PaymentTypeHelper
    {
        public static VippsPaymentType GetVippsPaymentType(IOrderGroup orderGroup)
        {
            if (orderGroup == null)
            {
                return VippsPaymentType.UNKNOWN;
            }

            if (Enum.TryParse<VippsPaymentType>(orderGroup?.Properties[VippsConstants.VippsPaymentTypeField]?.ToString(),
                out var paymentType))
            {
                return paymentType;
            }

            return VippsPaymentType.CHECKOUT;
        }
        
    }
}