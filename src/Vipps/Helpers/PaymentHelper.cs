using System;
using EPiServer.Commerce.Order;
using EPiServer.Globalization;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;

namespace Vipps.Helpers
{
    public static class PaymentHelper
    {
        public static PaymentMethodDto GetVippsPaymentMethodDto()
        {
            return PaymentManager.GetPaymentMethodBySystemName(VippsConstants.VippsSystemKeyword,
                ContentLanguage.PreferredCulture.Name);
        }

        public static IPayment CreateVippsPayment(ICart cart, decimal amount, Guid vippsPaymentMethodId)
        {
            var payment = cart.CreatePayment();
            payment.PaymentType = PaymentType.Other;
            payment.PaymentMethodId = vippsPaymentMethodId;
            payment.PaymentMethodName = VippsConstants.VippsSystemKeyword;
            payment.Amount = amount;
            payment.Status = PaymentStatus.Pending.ToString();
            payment.TransactionType = TransactionType.Authorization.ToString();

            return payment;
        }
    }
}
