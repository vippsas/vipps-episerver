namespace Vipps.Models
{
    public class ExpressCheckoutResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string RedirectUrl { get; set; }
    }
}
