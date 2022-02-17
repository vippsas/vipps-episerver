using System;
using Newtonsoft.Json;

namespace Vipps.Models.Partials
{
    public class Address
    {
        [JsonProperty("addressLine1")]
        public string AddressLine1 { get; set; }

        [JsonProperty("addressLine2")]
        public string AddressLine2 { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("postCode")]
        public string PostCode { get; set; }

        [Obsolete("This property is not in use and will be removed in a future release. Use PostCode instead")]
        [JsonProperty("zipCode")]
        public string ZipCode { get; set; }
    }
}
