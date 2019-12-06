using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vipps.Models
{
    public class ExpressCheckoutResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string RedirectUrl { get; set; }
    }
}
