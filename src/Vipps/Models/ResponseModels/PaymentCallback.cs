using Newtonsoft.Json;
using Vipps.Models.Partials;

namespace Vipps.Models.ResponseModels
{

    public class PaymentCallback
    {
        [JsonProperty("merchantSerialNumber")]
        public int MerchantSerialNumber { get; set; }

        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("shippingDetails")]
        public ShippingDetail ShippingDetails { get; set; }

        [JsonProperty("transactionInfo")]
        public TransactionInfo TransactionInfo { get; set; }

        [JsonProperty("userDetails")]
        public UserDetails UserDetails { get; set; }
    }
}
