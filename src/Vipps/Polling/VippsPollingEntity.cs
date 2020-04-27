using System;
using System.ComponentModel.DataAnnotations;

namespace Vipps.Polling
{
    public class VippsPollingEntity
    {
        [Key]
        public string OrderId { get; set; }
        public Guid ContactId { get; set; }
        public string CartName { get; set; }
        public string MarketId { get; set; }
        public DateTime Created { get; set; }
        public string InstanceId { get; set; }

        public VippsPollingEntity()
        {
            Created = DateTime.Now;
        }
    }
}
