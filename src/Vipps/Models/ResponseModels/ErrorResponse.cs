using Newtonsoft.Json;

namespace Vipps.Models.ResponseModels
{
    public class ErrorResponse
    {
        [JsonProperty("errorGroup")]
        public string ErrorGroup { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty("contextId")]
        public string ContextId { get; set; }
    }
}
