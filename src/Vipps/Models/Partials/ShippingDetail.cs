using Newtonsoft.Json;

namespace Vipps.Models.Partials
{
    public class ShippingDetail
    {
        [JsonProperty("address")]
        public Address Address { get; set; }

        [JsonProperty("shippingCost")]
        public double ShippingCost { get; set; }

        [JsonProperty("isDefault")]
        public bool IsDefault { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("shippingMethod")]
        public string ShippingMethod { get; set; }

        [JsonProperty("shippingMethodId")]
        public string ShippingMethodId { get; set; }

    }
}
