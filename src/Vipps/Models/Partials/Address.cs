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

        //GetOrderDetails api response uses postCode
        [JsonProperty("postCode")]
        public string PostCode { get; set; }

        //Express callback uses zipCode
        [JsonProperty("zipCode")]
        public string ZipCode { get; set; }
    }
}
