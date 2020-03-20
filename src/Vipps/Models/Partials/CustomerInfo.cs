using Newtonsoft.Json;

namespace Vipps.Models.Partials
{
    public class CustomerInfo
    {
        [JsonProperty("mobileNumber")]
        public string MobileNumber { get; set; }
    }
}
