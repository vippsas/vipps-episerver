using Newtonsoft.Json;

namespace Vipps.Models.RequestModels
{
    public class ShippingRequest
    {
        [JsonProperty("addressId")]
        public int AddressId { get; set; }

        [JsonProperty("addressLine1")]
        public string AddressLine1 { get; set; }

        [JsonProperty("addressLine2")]
        public string AddressLine2 { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("postalCode")]
        public string PostalCode { get; set; }

        [JsonProperty("postCode")]
        public string PostCode { get; set; }

        [JsonProperty("addressType")]
        public string AddressType { get; set; }
    }
}
