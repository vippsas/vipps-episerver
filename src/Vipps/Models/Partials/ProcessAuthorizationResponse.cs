using EPiServer.Commerce.Order;

namespace Vipps.Models.Partials
{
    public class ProcessAuthorizationResponse
    {
        public bool Processed { get; set; }
        public VippsPaymentType PaymentType {get; set; }
        public string ErrorMessage { get; set; }
        public IPurchaseOrder PurchaseOrder { get; set; }
    }
}
