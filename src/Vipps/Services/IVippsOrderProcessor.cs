using System;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using Vipps.Models;
using Vipps.Models.ResponseModels;

namespace Vipps.Services
{
    public interface IVippsOrderProcessor
    {
        ProcessOrderResponse CreatePurchaseOrder(ICart cart);

        Task<ProcessOrderResponse> FetchAndProcessOrderDetailsAsync(string orderId, Guid contactId, string marketId, string cartName);

        ProcessOrderResponse FetchAndProcessOrderDetails(string orderId, Guid contactId, string marketId, string cartName);

        [Obsolete("Please use another overload. This has performance concerns and will be removed in an upcoming version.")]
        Task<ProcessOrderResponse> ProcessOrderDetails(DetailsResponse detailsResponse, string orderId, Guid contactId,
            string marketId, string cartName);

        ProcessOrderResponse ProcessOrderDetails(DetailsResponse detailsResponse, string orderId, ICart cart);

        Task<ProcessOrderResponse> ProcessPaymentCallback(PaymentCallback paymentCallback, string orderId, string contactId,
            string marketId, string cartName);
    }
}
