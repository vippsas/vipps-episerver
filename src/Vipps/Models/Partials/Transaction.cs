using System;
using Newtonsoft.Json;

namespace Vipps.Models.Partials
{
    public class Transaction
    {
        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("timeStamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("transactionText")]
        public string TransactionText { get; set; }
    }
}
