using EPiServer.Commerce.Order;

namespace Vipps.Models
{
    public class ProcessAuthorizationResponse
    {
        public bool Processed { get; set; }
        public VippsPaymentType PaymentType { get; set; }
        public IPurchaseOrder PurchaseOrder { get; set; }
        public ProcessAuthorizationResponseError ProcessAuthorizationResponseError { get; set; }
    }
}