using Newtonsoft.Json;
using Vipps.Models.Partials;

namespace Vipps.Models.RequestModels
{
    public class InitiatePaymentRequest
    {
        [JsonProperty("customerInfo")]
        public CustomerInfo CustomerInfo { get; set; }

        [JsonProperty("merchantInfo")]
        public MerchantInfo MerchantInfo { get; set; }

        [JsonProperty("transaction")]
        public Transaction Transaction { get; set; }
    }
}
