using System;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using Vipps.Models;

namespace Vipps.Services
{
    public interface IVippsPaymentService
    {
        Task<PaymentProcessingResult> InitiateAsync(IOrderGroup orderGroup, IPayment payment);
        Task<PaymentProcessingResult> CaptureAsync(IOrderGroup orderGroup, IPayment payment);
        Task<PaymentProcessingResult> RefundAsync(IOrderGroup orderGroup, IPayment payment);
        Task<PaymentProcessingResult> CancelAsync(IOrderGroup orderGroup, IPayment payment);
        Task<ProcessAuthorizationResponse> ProcessAuthorizationAsync(Guid contactId, string marketId, string cartName,
            string orderId);
    }
}