using System.Collections.Generic;
using Newtonsoft.Json;
using Vipps.Models.Partials;

namespace Vipps.Models.ResponseModels
{
    public class ShippingDetailsResponse
    {
        [JsonProperty("addressId")]
        public int AddressId { get; set; }

        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("shippingDetails")]
        public IEnumerable<ShippingDetail> ShippingDetails { get; set; }
    }
}
