using System;
using EPiServer.Commerce.Order;

namespace Vipps.Services
{
    public interface IVippsService
    {
        ICart GetCartByContactId(string contactId, string marketId, string orderId);
        ICart GetCartByContactId(Guid contactId, string marketId, string orderId);
        IPurchaseOrder GetPurchaseOrderByOrderId(string orderId);
    }
}
