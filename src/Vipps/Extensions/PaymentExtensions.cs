using EPiServer.Commerce.Order;

namespace Vipps.Extensions
{
    public static class PaymentExtensions
    {
        public static bool IsVippsPayment(this IPayment payment)
        {
            return payment?.PaymentMethodName?.Equals(VippsConstants.VippsSystemKeyword) ?? false;
        }
    }
}