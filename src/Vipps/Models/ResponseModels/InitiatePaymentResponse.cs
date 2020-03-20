using Newtonsoft.Json;

namespace Vipps.Models.ResponseModels
{
    public class InitiatePaymentResponse
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
