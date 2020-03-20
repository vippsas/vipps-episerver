using Newtonsoft.Json;
using Vipps.Models.Partials;

namespace Vipps.Models.ResponseModels
{

    public class PaymentCallback : IVippsUserDetails
    {
        [JsonProperty("merchantSerialNumber")]
        public int MerchantSerialNumber { get; set; }

        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("shippingDetails")]
        public ShippingDetails ShippingDetails { get; set; }

        [JsonProperty("transactionInfo")]
        public TransactionInfo TransactionInfo { get; set; }

        [JsonProperty("userDetails")]
        public UserDetails UserDetails { get; set; }
    }
}
