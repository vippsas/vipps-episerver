using System;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using Vipps.Models;
using Vipps.Models.ResponseModels;

namespace Vipps.Services
{
    public interface IVippsOrderProcessor
    {
        Task<ProcessOrderResponse> CreatePurchaseOrder(ICart cart);

        Task<ProcessOrderResponse> ProcessOrderDetails(DetailsResponse detailsResponse, string orderId, Guid contactId,
            string marketId, string cartName);
        Task<ProcessOrderResponse> ProcessPaymentCallback(PaymentCallback paymentCallback, string orderId, string contactId,
            string marketId, string cartName);
    }
}
