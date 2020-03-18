using System;
using EPiServer.Commerce.Order;
using Vipps.Models.Partials;
using Vipps.Models.RequestModels;

namespace Vipps.Services
{
    public interface IVippsRequestFactory
    {
        InitiatePaymentRequest CreateInitiatePaymentRequest(IPayment payment, IOrderGroup orderGroup, VippsConfiguration configuration, string orderId, Guid contactId, string marketId);
        UpdatePaymentRequest CreateUpdatePaymentRequest(IPayment payment, VippsConfiguration configuration);

        UpdatePaymentRequest CreateUpdatePaymentRequest(VippsConfiguration configuration, TransactionLogHistory transactionLogHistory);
    }
}
