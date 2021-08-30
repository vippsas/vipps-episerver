using System;
using System.Linq;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Orders.Managers;

namespace Vipps.Extensions
{
    public static class PaymentExtensions
    {
        public static bool IsVippsPayment(this IPayment payment)
        {
            return (payment?.PaymentMethodName?.Equals(VippsConstants.VippsSystemKeyword) ?? false) || (MethodName(payment)?.Equals(VippsConstants.VippsSystemKeyword) ?? false);
        }

        private static string MethodName(this IPayment payment)
        {
            var id = payment?.PaymentMethodId ?? Guid.Empty;
            var method = PaymentManager.GetPaymentMethod(id, true)?.PaymentMethod.FirstOrDefault();
            return method?.SystemKeyword;
        } 
    }
}