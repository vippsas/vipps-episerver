using EPiServer.Commerce.Order;

namespace Vipps.Models
{
    public class ProcessOrderResponse
    {
        public IPurchaseOrder PurchaseOrder { get; set; }
        public ProcessResponseErrorType ProcessResponseErrorType { get; set; }
        public string ErrorMessage { get; set; }
    }
}
