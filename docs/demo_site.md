# Demo site

Either set up a local version of quicksilver (instructions [here](../demo/README.md)), or run through [Docker](https://github.com/Geta/package-shared/blob/master/README.md#local-development-set-up)

**Load and display payment**

- [_Vipps.cshtml](../demo/Sources/EPiServer.Reference.Commerce.Site/Views/Payment/_Vipps.cshtml) - display Vipps Payment method
- [_VippsConfirmation.cshtml](../demo/Sources/EPiServer.Reference.Commerce.Site/Views/Shared/_VippsConfirmation.cshtml) - Vipps order confirmation view
- [VippsPaymentMethod.cs](../demo/Sources/EPiServer.Reference.Commerce.Site/Features/Payment/PaymentMethods/VippsPaymentMethod.cs)

**Fallback controller**
Fallback controller can be found [here](../demo/Sources/EPiServer.Reference.Commerce.Site/Features/Checkout/Controllers/PaymentFallbackController.cs)

**Product pages**
A form has been added to [product index](../demo/Sources/EPiServer.Reference.Commerce.Site/Views/Product/Index.cshtml#L99) as well as the packages and bundle index.

**Cart preview**
A form has been added to [cart preview](../demo/Sources/EPiServer.Reference.Commerce.Site/Views/Shared/_MiniCartDetails.cshtml#L92)

**WishList page**
A form has been added to [product index](../demo/Sources/EPiServer.Reference.Commerce.Site/Views/WishList/Index.cshtml#L42)

**Frontend for Vipps Express Checkout api call**
Simple jquery/ajax [VippsExpress.js](../demo/Sources/EPiServer.Reference.Commerce.Site/Scripts/js/VippsExpress.js)

**Polling Initialization**
Initialize polling on the site as in example above [VippsPollingInitialization.cs](../demo/Sources/EPiServer.Reference.Commerce.Site/Infrastructure/VippsPollingInitialization.cs)