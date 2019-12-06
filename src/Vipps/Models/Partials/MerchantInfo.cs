using Newtonsoft.Json;

namespace Vipps.Models.Partials
{
    public class MerchantInfo
    {
        [JsonProperty("authToken")]
        public string AuthToken { get; set; }

        [JsonProperty("callbackPrefix")]
        public string CallbackPrefix { get; set; }

        [JsonProperty("consentRemovalPrefix")]
        public string ConsentRemovalPrefix { get; set; }

        [JsonProperty("fallBack")]
        public string FallBack { get; set; }

        [JsonProperty("isApp")]
        public bool IsApp { get; set; }

        [JsonProperty("merchantSerialNumber")]
        public int MerchantSerialNumber { get; set; }

        [JsonProperty("paymentType")]
        public string PaymentType { get; set; }

        [JsonProperty("shippingDetailsPrefix")]
        public string ShippingDetailsPrefix { get; set; }
    }
}
