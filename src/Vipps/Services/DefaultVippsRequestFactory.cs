using System;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Vipps.Helpers;
using Vipps.Models;
using Vipps.Models.Partials;
using Vipps.Models.RequestModels;

namespace Vipps.Services
{
    [ServiceConfiguration(typeof(IVippsRequestFactory))]
    public class DefaultVippsRequestFactory : IVippsRequestFactory
    {
        public virtual InitiatePaymentRequest CreateInitiatePaymentRequest(IPayment payment, IOrderGroup orderGroup, VippsConfiguration configuration, string orderId, Guid contactId, string marketId)
        {
            return new InitiatePaymentRequest
            {
                CustomerInfo = new CustomerInfo
                {
                    MobileNumber = GetPhoneNumberFromBillingAddress(payment.BillingAddress)
                },
                MerchantInfo = new MerchantInfo
                {
                    CallbackPrefix = EnsureCorrectUrl(configuration.SiteBaseUrl, $"vippscallback/{contactId.ToString()}/{marketId}/{orderGroup.Name}"),
                    ConsentRemovalPrefix = EnsureCorrectUrl(configuration.SiteBaseUrl, $"vippscallback/"),
                    ShippingDetailsPrefix = EnsureCorrectUrl(configuration.SiteBaseUrl, $"vippscallback/{contactId.ToString()}/{marketId}/{orderGroup.Name}"),
                    FallBack = $"{configuration.FallbackUrl}?orderId={orderId}&contactId={contactId.ToString()}&marketId={marketId}&cartName={orderGroup.Name}",
                    IsApp = false,
                    MerchantSerialNumber = Convert.ToInt32(configuration.MerchantSerialNumber),
                    PaymentType = GetCheckoutType(orderGroup)
                    
                },
                Transaction = new Transaction
                {
                    Amount = payment.Amount.FormatAmountToVipps(),
                    OrderId = orderId,
                    TimeStamp = DateTime.Now,
                    TransactionText = configuration.TransactionMessage
                }
            };
        }

        public virtual UpdatePaymentRequest CreateUpdatePaymentRequest(IPayment payment,
            VippsConfiguration configuration)
        {
            return new UpdatePaymentRequest
            {
                MerchantInfo = new MerchantInfo
                {
                    MerchantSerialNumber = Convert.ToInt32(configuration.MerchantSerialNumber)
                },
                Transaction = new Transaction
                {
                    Amount = payment.Amount.FormatAmountToVipps(),
                    TransactionText = configuration.TransactionMessage
                }
            };
        }

        public virtual UpdatePaymentRequest CreateUpdatePaymentRequest(VippsConfiguration configuration, TransactionLogHistory transactionLogHistory)
        {
            return new UpdatePaymentRequest
            {
                MerchantInfo = new MerchantInfo
                {
                    MerchantSerialNumber = Convert.ToInt32(configuration.MerchantSerialNumber)
                },
                Transaction = new Transaction
                {
                    Amount = transactionLogHistory.Amount,
                    TransactionText = configuration.TransactionMessage
                }
            };
        }

        private string GetPhoneNumberFromBillingAddress(IOrderAddress billingAddress)
        {
            return !string.IsNullOrEmpty(billingAddress.DaytimePhoneNumber)
                ? PhoneNumberHelper.Validate(billingAddress.DaytimePhoneNumber)
                : PhoneNumberHelper.Validate(billingAddress.EveningPhoneNumber);
        }

        private static string EnsureCorrectUrl(string siteBaseUrl, string path)
        {
            return siteBaseUrl.EndsWith("/") ? $"{siteBaseUrl}{path}" : $"{siteBaseUrl}/{path}";
        }

        private static string GetCheckoutType(IOrderGroup cart)
        {
            if (Enum.TryParse<VippsPaymentType>(cart.Properties[VippsConstants.VippsPaymentTypeField]?.ToString(),
                out var paymentType))
            {
                if(paymentType == VippsPaymentType.CARTEXPRESS || paymentType == VippsPaymentType.PRODUCTEXPRESS || paymentType == VippsPaymentType.WISHLISTEXPRESS)
                    return VippsConstants.ExpressCheckout;
            }

            return VippsConstants.RegularCheckout;
        }
    }
}
