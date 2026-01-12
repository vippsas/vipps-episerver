<!-- START_METADATA
---
title: Optimizely FAQ
sidebar_label: FAQ
sidebar_position: 60
pagination_next: null
pagination_prev: null
section: Plugins
---
END_METADATA -->

# Frequently asked questions

## I'm having issues with one payment creating two orders

First, make sure that `IVippsPollingService`, `IVippsOrderProcessor` and `IVippsOrderSynchronizer` are all registered as singletons.

In a load balanced environment, this might be a slightly more complex issue. Out of the box, the package works only with Azure (and therefore DXC) load balanced environments.

For example, a user could place an order and get redirected back from Vipps to the fallback controller on the original instance they placed the order from.
At the same time, a callback from Vipps could hit the other server's callback controller.

In an Azure environment, we validate the request based on the current `InstanceId` and the `ARRAffinity` cookie.
If the request has gone to the wrong instance, we set the cookie and send the request back to the same controller. [VippsCallbackController.cs](https://github.com/vippsas/vipps-episerver/tree/master/src/Vipps/Controllers/VippsCallbackController.cs#L97)

So what do you do if you are in a load balanced environment that is not hosted in azure?
Well, with the prerequisite that your sticky session is handled by a cookie with a static name, and the value of the cookie being retrievable from the backend you could:

* Override `DefaultVippsOrderSynchronizer` and make sure [GetInstanceId()](https://github.com/vippsas/vipps-episerver/tree/master/src/Vipps/Services/DefaultVippsOrderSynchronizer.cs#L132) returns the value stored in the cookie used for sticky sessions.
* Add `Vipps:InstanceCookieName` app setting with the name of the cookie as value

And you should be good to go.

### How do I add Vipps as an external payment method in Klarna

This is quite easy. Think of this as a hybrid between Vipps as a payment method in checkout and Vipps Express.

* Create an action in an MVC controller that takes, for example, `orderGroupId` as a parameter (or `customerId`, `marketId` and `cartName`).
* Add this URL to the external payment method in Klarna.
* Load the cart.
* Fetch the Klarna order that is associated with the cart.
* Create the Vipps payment like it's done in [VippsExpressController.cs](https://github.com/vippsas/vipps-episerver/tree/master/src/Vipps/Controllers/VippsExpressController.cs#L95).
* Run `ProcessPayments()` and redirect to the `PaymentRedirectResults` `RedirectUrl`, which is hopefully successful.

One thing to keep in mind is to clear the payments from the order form before adding a new payment. If the transaction is cancelled by customer or something goes wrong, we don't want to keep adding new payments to the form.
