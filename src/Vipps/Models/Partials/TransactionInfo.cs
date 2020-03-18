using System;
using Newtonsoft.Json;

namespace Vipps.Models.Partials
{
    public class TransactionInfo : IVippsPaymentDetails
    {
        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("timeStamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }
    }
}
