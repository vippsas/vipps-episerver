using Vipps.Models.Partials;

namespace Vipps.Models
{
    public interface IVippsUserDetails
    {
        UserDetails UserDetails { get; set; }
        ShippingDetails ShippingDetails { get; set; }
    }
}
