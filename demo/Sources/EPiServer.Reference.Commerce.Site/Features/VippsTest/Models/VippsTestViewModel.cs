using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Vipps;
using Vipps.Models;

namespace EPiServer.Reference.Commerce.Site.Features.VippsTest.Models
{
    public class VippsTestViewModel
    {
        public string Step { get; set; }
        public string Message { get; set; }
        public VippsTestForm VippsTestForm { get; set; }
    }

    public class VippsTestForm
    {
        public string OrderId { get; set; }
    }
}