using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using Vipps.Models;

namespace Vipps.Services
{
    public interface IVippsOrderCreator
    {
        Task<LoadOrCreatePurchaseOrderResponse> LoadOrCreatePurchaseOrder(ICart cart, string orderId);
        Task<LoadOrCreatePurchaseOrderResponse> CreatePurchaseOrder(ICart cart);
    }
}
