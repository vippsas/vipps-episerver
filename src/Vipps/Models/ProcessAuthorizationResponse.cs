namespace Vipps.Models
{
    public class ProcessAuthorizationResponse : ProcessOrderResponse
    {
        public bool Processed { get; set; }

        public ProcessAuthorizationResponse(ProcessOrderResponse processOrderResponse)
        {
            ErrorMessage = processOrderResponse.ErrorMessage;
            ProcessResponseErrorType = processOrderResponse.ProcessResponseErrorType;
            PurchaseOrder = processOrderResponse.PurchaseOrder;
            PaymentType = processOrderResponse.PaymentType;
        }

        public ProcessAuthorizationResponse() { }
    }
}
