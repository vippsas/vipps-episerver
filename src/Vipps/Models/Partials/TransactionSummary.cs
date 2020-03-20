using Newtonsoft.Json;

namespace Vipps.Models.Partials
{
    public class TransactionSummary
    {
        [JsonProperty("capturedAmount")]
        public int CapturedAmount { get; set; }

        [JsonProperty("refundedAmount")]
        public int RefundedAmount { get; set; }

        [JsonProperty("remainingAmountToCapture")]
        public int RemainingAmountToCapture { get; set; }

        [JsonProperty("remainingAmountToRefund")]
        public int RemainingAmountToRefund { get; set; }
    }
}
