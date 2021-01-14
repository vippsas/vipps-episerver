# Demo site

Either run through Docker or set up project locally

## Docker setup
 - Set a sa_password (to whatever you want, just comply with [this](https://docs.microsoft.com/en-us/sql/relational-databases/security/password-policy?view=sql-server-ver15) ) in [docker_compose.yml](../demo/docker_compose.yml)
 - Set the same password in connectionstrings.dev.config located in both EPiServer.Reference.Commerce.Manager and EPiServer.Reference.Commerce.Site folders
 - Create a new developer find index and set values in EPiServerFind.dev.config located in EPiServer.Reference.Commerce.Site
 - Run the project (F5)
 - If docker is not previously installed/configured on your local environment, Visual studio should prompt you to do so.
 - Navigate to http://172.16.238.11/ or http://172.16.238.12/ for Commerce Manager

## Local dev setup
 - Restore databases located [here](../demo/Sources/EPiServer.Reference.Commerce.Site/App_Data)
 - Set correct info on connectionstrings.dev.config located in both EPiServer.Reference.Commerce.Manager and EPiServer.Reference.Commerce.Site folders
 - Create a new developer find index and set values in EPiServerFind.dev.config located in EPiServer.Reference.Commerce.Site
 - Run project

**Load and display payment**

- [_Vipps.cshtml](../demo/Sources/EPiServer.Reference.Commerce.Site/Views/Payment/_Vipps.cshtml) - display Vipps Payment method
- [_VippsConfirmation.cshtml](../demo/Sources/EPiServer.Reference.Commerce.Site/Views/Shared/_VippsConfirmation.cshtml) - Vipps order confirmation view
- [VippsPaymentMethod.cs](../demo/Sources/EPiServer.Reference.Commerce.Site/Features/Payment/PaymentMethods/VippsPaymentMethod.cs)

**Fallback controller**
Fallback controller can be found [here](demo/Sources/EPiServer.Reference.Commerce.Site/Features/Checkout/Controllers/PaymentFallbackController.cs)

**Product pages**
A form has been added to [product index](../demo/Sources/EPiServer.Reference.Commerce.Site/Views/Product/Index.cshtml#L99) as well as the packages and bundle index.

**Cart preview**
A form has been added to [cart preview](../demo/Sources/EPiServer.Reference.Commerce.Site/Views/Shared/_MiniCartDetails.cshtml#L92)

**WishList page**
A form has been added to [product index](../demo/Sources/EPiServer.Reference.Commerce.Site/Views/WishList/Index.cshtml#L42)

**Frontend for Vipps Express Checkout api call**
Simple jquery/ajax [VippsExpress.js](demo/Sources/EPiServer.Reference.Commerce.Site/Scripts/js/VippsExpress.js)

**Polling Initialization**
Initialize polling on the site as in example above [VippsPollingInitialization.cs](../demo/Sources/EPiServer.Reference.Commerce.Site/Infrastructure/VippsPollingInitialization.cs)

# Local development environment

In order to use / work on this package locally you'll need a tool called [ngrok](https://www.ngrok.com). This tool can forward a generated ngrok URL to a localhost URL. Both Vipps regular payments as well as express payments are dependant on callbacks from Vipps.