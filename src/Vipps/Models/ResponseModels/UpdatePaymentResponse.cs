using Newtonsoft.Json;
using Vipps.Models.Partials;

namespace Vipps.Models.ResponseModels
{
    public class UpdatePaymentResponse
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }
        
        [JsonProperty("transactionInfo")]
        public TransactionInfo TransactionInfo { get; set; }

        [JsonProperty("transactionSummary")]
        public TransactionSummary TransactionSummary { get; set; }

    }
}
