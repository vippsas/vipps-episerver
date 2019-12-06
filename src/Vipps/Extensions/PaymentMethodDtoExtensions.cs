using Mediachase.Commerce.Orders.Dto;

namespace Vipps.Extensions
{
    public static class PaymentMethodDtoExtensions
    {
        public static VippsConfiguration GetVippsConfiguration(this PaymentMethodDto paymentMethod)
        {
            return new VippsConfiguration
            {
                ClientId = paymentMethod.GetParameter(VippsConstants.ClientId, string.Empty),
                ClientSecret = paymentMethod.GetParameter(VippsConstants.ClientSecret, string.Empty),
                SubscriptionKey = paymentMethod.GetParameter(VippsConstants.SubscriptionKey, string.Empty),
                MerchantSerialNumber = paymentMethod.GetParameter(VippsConstants.MerchantSerialNumber, string.Empty),
                ApiUrl = paymentMethod.GetParameter(VippsConstants.ApiUrl, string.Empty),
                SiteBaseUrl = paymentMethod.GetParameter(VippsConstants.SiteBaseUrl, string.Empty),
                FallbackUrl = paymentMethod.GetParameter(VippsConstants.FallbackUrl, string.Empty),
                TransactionMessage = paymentMethod.GetParameter(VippsConstants.TransactionMessage, string.Empty)
            };
        }
    }
}
