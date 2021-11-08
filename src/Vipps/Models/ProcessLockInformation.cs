using System;

namespace Vipps.Models
{
    internal class ProcessLockInformation
    {
        public ProcessLockInformation(string orderId, int orderGroupId)
        {
            OrderId = orderId;
            OrderGroupId = orderGroupId;
        }

        public ProcessLockInformation(string orderId, Guid contactId, string marketId, string cartName)
        {
            OrderId = orderId;
            ContactId = contactId;
            MarketId = marketId;
            CartName = cartName;
        }

        public string OrderId { get; }
        public int? OrderGroupId { get; }
        public Guid? ContactId { get; }
        public string MarketId { get; }
        public string CartName { get; }
    }
}
