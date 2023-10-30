<!-- START_METADATA
---
title: Install and configure Optimizely
sidebar_label: Install and configure
sidebar_position: 10
pagination_next: null
pagination_prev: null
---
END_METADATA -->

# Install and configure

## Installation

Start by installing NuGet packages (use [NuGet](https://nuget.episerver.com/)):

    Install-Package Vipps

For the Commerce Manager site run the following package:

    Install-Package Vipps.CommerceManager

## Configure Commerce Manager

Login into Commerce Manager and open **Administration -> Order System -> Payments**. Add new payment.

## Local development environment

In order to use / work on this package locally you'll need a tool called [ngrok](https://www.ngrok.com). This tool can forward a generated ngrok URL to a localhost URL. Both Vipps regular payments as well as express payments are dependent on callbacks from Vipps.

### Overview tab

- *Name(\*)*
- *System Keyword(\*)* - Vipps (the integration will not work when something else is entered in this field)
- *Language(\*)* - Allows a specific language to be specified for the payment gateway
- *Class Name(\*)* - Choose *Vipps.VippsPaymentGateway*
- *Payment Class(\*)* - Choose *Mediachase.Commerce.Orders.OtherPayment*
- *IsActive* - *Yes*
- *Supports Recurring* - **No** - Vipps recurring payments are not yet supported

(\*) mandatory

- select shipping methods available for this payment

![Payment method settings](screenshots/payment-overview.png "Payment method settings")

### Parameters

 - *Client Id* - Can be obtained through [portal.vipps.no](https://portal.vipps.no).
 - *Client Secret*- Can be obtained through [portal.vipps.no](https://portal.vipps.no).
 - *Subscription Key* - Can be obtained through [portal.vipps.no](https://portal.vipps.no).
 - *Serial number* - Your merchant Serial number, can be obtained through [portal.vipps.no](https://portal.vipps.no).
 - *System name* - A vendor specific identifier, usually the company name like `acme`, [more here](https://developer.vippsmobilepay.com/docs/knowledge-base/http-headers/).
 - *Api Url* - Vipps API URL (test or prod).
 - *Site Base Url* - The URL for your site (used to generate callback URLs, ngrok generated URL if running local dev env).
 - *Fallback Url* - URL to your fallback controller.

![Payment method parameters](screenshots/payment-parameters.png "Payment method settings")

## Initialization

In your initialization module you must register the following interfaces:
```cs
services.AddTransient<IVippsService, VippsService>();
services.AddTransient<IVippsPaymentService, VippsPaymentService>();
services.AddTransient<IVippsRequestFactory, DefaultVippsRequestFactory>();
services.AddTransient<IVippsResponseFactory, DefaultVippsResponseFactory>();
services.AddTransient<IVippsAsyncPaymentService, VippsAsyncPaymentService>();
services.AddSingleton<IVippsOrderSynchronizer, DefaultVippsOrderSynchronizer>();
services.AddSingleton<IVippsOrderProcessor, DefaultVippsOrderProcessor>();
services.AddSingleton<IVippsPollingService, VippsPollingService>();
```

It is important that `IVippsOrderProcessor`, `IVippsPollingService` and `IVippsOrderSynchronizer` are registered as singletons.

## Fallback controller

Must be implemented in your project.

The package automatically appends the generated order ID as a query string to the specified URL. The quicksilver example implementation can be found [here](https://github.com/vippsas/vipps-episerver/blob/master/demo/Sources/EPiServer.Reference.Commerce.Site/Features/Checkout/Controllers/PaymentFallbackController.cs).

`ProcessAuthorizationAsync` method on `IVippsAsyncPaymentService` will return the created purchase order for you if the callback from Vipps was successful. If not, it will ensure all the correct information is on the payment and shipment objects and then create the purchase order.
**Please note:** No validation against tempering with the cart line items is done within the package.

```cs
var result = await _vippsAsyncPaymentService.ProcessAuthorizationAsync(currentContactId, currentMarketId, cartName, orderId);
```

The method returns a `ProcessAuthorizationResponse` which contains an enum called `VippsPaymentType`, this can be set to:

 - CHECKOUT - Payment was initiated from checkout page
 - PRODUCTEXPRESS - Payment was initiated from product page
 - CARTEXPRESS - Payment was initiated from cart page/preview
 - WISHLISTEXPRESS - Payment was initiated from wishlist page/preview
 - UNKNOWN - Cart can't be found

This determines where the fallback controller should redirect if `processAuthorizationResult.Processed = false`
Back to the checkout, product, wishlist, or cart page.

If the payment is processed and the paymenttype is `WISHLISTEXPRESS`, you might also consider finding the customers wishlist cart and deleting it in the fallback controller.

**Please note:** This only applies to Express payments. If you are only using Vipps in the checkout, VippsPaymentType will always be `CHECKOUT` and the redirect action will be determined based on whether the payment succeeded or not.

The `ProcessAuthorizationResponse` also contains a possible error message as well as a `ProcessResponseErrorType` enum.
 - NONE
 - NOCARTFOUND
 - NOVIPPSPAYMENTINCART
 - FAILED
 - ORDERVALIDATIONERROR
 - EXCEPTION
 - OTHER

## Order validation

No order validation is included in this package to protect from f.ex. cart
tempering. It is **highly** recommended that you implement your own order validation.
Override the `CreatePurchaseOrder` method in the `DefaultVippsOrderProcessor` class.

## Polling

The package includes polling the Vipps API to ensure that the payment is handled, even if user closes the browser tab before redirect and a callback from Vipps is not received.
 - Polling is started when a user is redirected to Vipps.
 - Polling is active for up to ten minutes
 - If a payment has a status that we can act upon polling stops.
 - Set polling interval by adding `Vipps:PollingInterval` app setting in web config (in milliseconds). Default is 2000 ms.

### Initialize polling
```cs
[InitializableModule]
[ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
internal class VippsPollingInitialization : IInitializableModule
{
	public void Initialize(InitializationEngine context)
    {
       PollingInitialization.Initialize(context);
    }

    public void Uninitialize(InitializationEngine context)
    {
		
    }
}
```

**Example:** (assuming `MyOrderService` handles all the order validation)

```cs
public override async Task < ProcessOrderResponse > CreatePurchaseOrder(ICart cart) {
	try {
		var response = _myOrderService.CreatePurchaseOrder(cart);

		if (response.Success) {
			return new ProcessOrderResponse {
				PurchaseOrder = response.PurchaseOrder
			};
		}

		return new ProcessOrderResponse {
			ProcessResponseErrorType = ProcessResponseErrorType.ORDERVALIDATIONERROR,
			ErrorMessage = response.Message
		};
	}
	catch(Exception ex) {
		_logger.Error(ex.Message);
		return new ProcessOrderResponse {
			ErrorMessage = ex.Message,
			ProcessResponseErrorType = ProcessResponseErrorType.EXCEPTION
		};
	}
}
```
