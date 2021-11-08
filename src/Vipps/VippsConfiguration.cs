namespace Vipps
{
    public class VippsConfiguration
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string SubscriptionKey { get; set; }
        public string MerchantSerialNumber { get; set; }
        public string SystemName { get; set; }
        public string ApiUrl { get; set; }
        public string SiteBaseUrl { get; set; }
        public string FallbackUrl {get; set; }
        public string TransactionMessage { get; set; }
        public string MarketId { get; set; }
    }
}
