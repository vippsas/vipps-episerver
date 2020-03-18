using System.Collections.Generic;
using Newtonsoft.Json;
using Vipps.Models.Partials;

namespace Vipps.Models.ResponseModels
{

    public class DetailsResponse : IVippsUserDetails
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("shippingDetails")]
        public ShippingDetails ShippingDetails { get; set; }

        [JsonProperty("transactionLogHistory")]
        public IEnumerable<TransactionLogHistory> TransactionLogHistory { get; set; }

        [JsonProperty("transactionSummary")]
        public TransactionSummary TransactionSummary { get; set; }

        [JsonProperty("userDetails")]
        public UserDetails UserDetails { get; set; }
    }
}
