<!-- START_METADATA
---
title: Optimizely Demo site
sidebar_label: Demo site
sidebar_position: 30
pagination_next: plugins-ext/episerver/docs/checklist
pagination_prev: plugins-ext/episerver/docs/express_checkout
section: Plugins
---
END_METADATA -->

# Demo site

Either set up a local version of quicksilver ([instructions on GitHub](https://github.com/vippsas/vipps-episerver/blob/master/demo/README.md)), or run through [Docker](https://github.com/Geta/package-shared/blob/master/README.md#local-development-set-up).

## Load and display payment

- [_Vipps.cshtml](https://github.com/vippsas/vipps-episerver/blob/master/demo/Sources/EPiServer.Reference.Commerce.Site/Views/Payment/_Vipps.cshtml) - display Vipps Payment method
- [_VippsConfirmation.cshtml](https://github.com/vippsas/vipps-episerver/blob/master/demo/Sources/EPiServer.Reference.Commerce.Site/Views/Shared/_VippsConfirmation.cshtml) - Vipps order confirmation view
- [VippsPaymentMethod.cs](https://github.com/vippsas/vipps-episerver/blob/master/demo/Sources/EPiServer.Reference.Commerce.Site/Features/Payment/PaymentMethods/VippsPaymentMethod.cs)

## Fallback controller

Fallback controller can be found [on GitHub](https://github.com/vippsas/vipps-episerver/blob/master/demo/Sources/EPiServer.Reference.Commerce.Site/Features/Checkout/Controllers/PaymentFallbackController.cs).

## Product pages

A form has been added to [product index](https://github.com/vippsas/vipps-episerver/blob/master/demo/Sources/EPiServer.Reference.Commerce.Site/Views/Product/Index.cshtml#L99) as well as the packages and bundle index.

## Cart preview

A form has been added to [cart preview](https://github.com/vippsas/vipps-episerver/blob/master/demo/Sources/EPiServer.Reference.Commerce.Site/Views/Shared/_MiniCartDetails.cshtml#L92).

## WishList page

A form has been added to [product index](https://github.com/vippsas/vipps-episerver/blob/master/demo/Sources/EPiServer.Reference.Commerce.Site/Views/WishList/Index.cshtml#L42).

## Frontend for Vipps Express Checkout API call

Simple `jquery/ajax` [VippsExpress.js](https://github.com/vippsas/vipps-episerver/blob/master/demo/Sources/EPiServer.Reference.Commerce.Site/Scripts/js/VippsExpress.js).

## Polling initialization

Initialize polling on the site as in example above [VippsPollingInitialization.cs](https://github.com/vippsas/vipps-episerver/blob/master/demo/Sources/EPiServer.Reference.Commerce.Site/Infrastructure/VippsPollingInitialization.cs).
