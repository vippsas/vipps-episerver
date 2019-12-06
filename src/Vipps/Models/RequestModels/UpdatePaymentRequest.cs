using Newtonsoft.Json;
using Vipps.Models.Partials;

namespace Vipps.Models.RequestModels
{
    public class UpdatePaymentRequest
    {
        [JsonProperty("merchantInfo")]
        public MerchantInfo MerchantInfo { get; set; }

        [JsonProperty("transaction")]
        public Transaction Transaction { get; set; }
    }
}
