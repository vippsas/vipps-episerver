using System;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using Vipps.Models.Partials;
using Vipps.Models.ResponseModels;

namespace Vipps.Services
{
    public interface IVippsPaymentService
    {
        Task<PaymentProcessingResult> InitiateAsync(IOrderGroup orderGroup, IPayment payment);
        Task<PaymentProcessingResult> CaptureAsync(IOrderGroup orderGroup, IPayment payment, IShipment shipment);
        Task<PaymentProcessingResult> RefundAsync(IOrderGroup orderGroup, IPayment payment, IShipment shipment);
        Task<PaymentProcessingResult> CancelAsync(IOrderGroup orderGroup, IPayment payment, IShipment shipment);
        Task<ProcessAuthorizationResponse> ProcessAuthorizationAsync(Guid contactId, string marketId, string orderId);
        Task<DetailsResponse> GetOrderDetailsAsync(string orderId);
        Task<StatusResponse> GetOrderStatusAsync(string orderId);
    }
}