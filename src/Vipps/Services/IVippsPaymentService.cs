using System;
using EPiServer.Commerce.Order;
using Vipps.Models;

namespace Vipps.Services
{
    public interface IVippsPaymentService
    {
        PaymentProcessingResult Initiate(IOrderGroup orderGroup, IPayment payment);
        PaymentProcessingResult Capture(IOrderGroup orderGroup, IPayment payment);
        PaymentProcessingResult Refund(IOrderGroup orderGroup, IPayment payment);
        PaymentProcessingResult Cancel(IOrderGroup orderGroup, IPayment payment);
        ProcessAuthorizationResponse ProcessAuthorization(Guid contactId, string marketId, string cartName,
            string orderId);
    }
}