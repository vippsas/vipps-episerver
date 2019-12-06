namespace Vipps
{
    public static class VippsConstants
    {
        public const string VippsSystemKeyword = "Vipps";

        public const string VippsDefaultCartName = "Default";
        public const string VippsSingleProductCart = "VippsSingleProductCart";

        public const string OrderNamespace = "Mediachase.Commerce.Orders";
        public const string PurchaseOrderClass = "PurchaseOrder";
        public const string CartClass = "ShoppingCart";

        // PurchaseOrder/Cart meta fields
        public const string VippsOrderIdField = "VippsOrderId";
        public const string VippsPaymentTypeField = "VippsPaymentType";

        // Payment meta fields
        public const string ClientId = "VippsClientId";
        public const string ClientSecret = "VippsClientSecret";
        public const string SubscriptionKey = "VippsSubscriptionKey";
        public const string MerchantSerialNumber = "VippsMerchantSerialNumber";
        public const string ApiUrl = "VippsApiUrl";
        public const string SiteBaseUrl = "VippsSiteBaseUrl";
        public const string FallbackUrl = "VippsFallbackUrl";
        public const string TransactionMessage = "VippsTransactionMessage";

        //Payment variables
        public const string ExpressCheckout = "eComm Express Payment";
        public const string RegularCheckout = "eComm Regular Payment";

    }
}
