using System;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using Vipps.Models.ResponseModels;

namespace Vipps.Services
{
    public interface IVippsService
    {
        ICart GetCartByContactId(string contactId, string marketId, string cartName);
        ICart GetCartByContactId(Guid contactId, string marketId, string cartName);
        IPurchaseOrder GetPurchaseOrderByOrderId(string orderId);
        Task<DetailsResponse> GetOrderDetailsAsync(string orderId, string marketId);
        Task<StatusResponse> GetOrderStatusAsync(string orderId, string marketId);
    }
}
