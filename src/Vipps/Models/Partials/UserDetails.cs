using Newtonsoft.Json;

namespace Vipps.Models.Partials
{
    public class UserDetails
    {
        [JsonProperty("bankIdVerified")]
        public string BankIdVerified { get; set; }

        [JsonProperty("dateOfBirth")]
        public string DateOfBirth { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("mobileNumber")]
        public string MobileNumber { get; set; }

        [JsonProperty("ssn")]
        public string Ssn { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }
    }
}
