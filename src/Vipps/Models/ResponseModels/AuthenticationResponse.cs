using System;
using Newtonsoft.Json;

namespace Vipps.Models.ResponseModels
{
    public class AuthenticationResponse
    {
        private readonly DateTime _createdAt;

        public AuthenticationResponse()
        {
            _createdAt = DateTime.Now;
        }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public string ExpiresIn { get; set; }

        [JsonProperty("ext_expires_in")]
        public string ExtExpiresIn { get; set; }

        [JsonProperty("expires_on")]
        public string ExpiresOn { get; set; }

        [JsonProperty("not_before")]
        public string NorBefore { get; set; }

        [JsonProperty("resource")]
        public string Resources { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        public bool IsExpired()
        {
            // Add a few seconds to DateTime.Now because we'd rather refresh the token a bit too early
            return DateTime.Now.AddSeconds(30) > _createdAt.AddSeconds(Convert.ToInt32(ExpiresIn));
        }

        public string MarketId { get; set; }
    }
}
