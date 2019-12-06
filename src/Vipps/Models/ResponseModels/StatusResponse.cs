using Newtonsoft.Json;
using Vipps.Models.Partials;

namespace Vipps.Models.ResponseModels
{
    public class StatusResponse
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("transactionInfo")]
        public TransactionInfo TransactionInfo { get; set; }
    }
}
