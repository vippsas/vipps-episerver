using EPiServer.Commerce.Order;

namespace Vipps.Models
{
    public class LoadOrCreatePurchaseOrderResponse
    {
        public IPurchaseOrder PurchaseOrder { get; set; }
        public string ErrorMessage { get; set; }
        public bool PurchaseOrderCreated { get; set; }
    }
}
