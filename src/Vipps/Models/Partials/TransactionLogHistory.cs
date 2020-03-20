using System;
using Newtonsoft.Json;

namespace Vipps.Models.Partials
{
    public class TransactionLogHistory : IVippsPaymentDetails
    {
        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("operation")]
        public string Operation { get; set; }

        [JsonProperty("operationSuccess")]
        public bool OperationSuccess { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("timeStamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("transactionText")]
        public string TransactionText { get; set; }
    }
}
