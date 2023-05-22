# Express Checkout

- [Get started](#get-started)
- [Callbacks](#callbacks)
- [Express workflows](#express-workflows)

## Get started

This section assumes you have already installed and configured the Vipps payment method as described [here](configure.md)

### Express controller

An API controller for initializing an express checkout is included in the package. This controller contains basic add to cart functionality for express checkout on product pages, but if you want to use your own cart workflow you will need to create your own controller for this.

If you want to use the express checkout in your cart preview, the controller will try and look for a cart named "Default". So if the default cart name for your site is something else, you also need to implement your own controller.

The controller has three methods:
 - GET vippsexpress/cartexpress
 - GET vippsexpress/wishlistexpress
 - POST vippsexpress/productexpress?code={code}&quantity={quantity}

 In return, you get a `ExpressCheckoutResponse` with three properties
  - Success
  - ErrorMessage
  - RedirectUrl

### Frontend

For the simplest possible frontend implementation of this using jQuery and AJAX. See [VippsExpress.js](../demo/Sources/EPiServer.Reference.Commerce.Site/Scripts/js/VippsExpress.js) and [Product/Index](../demo/Sources/EPiServer.Reference.Commerce.Site/Views/Product/Index.cshtml)

### Implement your own Express Checkout (Api) Controller

If you need to implement your own version of the Express Checkout Controller, which in a majority of cases would be the recommended route, there are a few things that are important to keep in mind:

**Cart metafield "VippsPaymentType" must be set before processing the payment**
This is how we differentiate express payments from regular checkout payments, as well as how we determine the redirect action in the `ProcessAuthorizationResponse` so the actual processing of the payment will go wrong if this metafield is not set. This metafiled key is located in `VippsConstants.VippsPaymentType` and the avaliable Values in `VippsPaymentType` enum.

**Cart Name**
In the included implementation of Vipps Express on product page, the cart name is "VippsSingleProductCart". This string can be found in `VippsConstants.VippsSingleProductCart`. This is because we don't want to delete the users "Default" cart when using the Express Checkout.
If you're creating your own implementation, the cart name can be anything you choose since it is passed back to us in the callback and fallback urls.

**Clear cart payments before adding a new payment**
It's assumed that a cart only has one Vipps payment associated with it.

**PaymentHelper**
PaymentHelper will help you create and add a Vipps payment to the cart. It has two helpful methods:
 - `PaymentHelper.GetVippsPaymentMethodDto();` will get the `PaymentMethodDto` for Vipps
 - `PaymentHelper.CreateVippsPayment(ICart, Money, PaymentMethodDto);` will return a Vipps `IPayment` you will be able to add to your cart.
 
 Example of the default VippsExpressController is found [here](../src/Vipps/Controllers/VippsExpressController.cs)

## Callbacks

The code being run on all callbacks is in `DefaultVippsResponseFactory`. 
If you have any needs to set properties on shipment, f.ex. setting a drop point or customize any other behaviour on express callbacks this is the place to do it.
Just create a new class that inherits from `DefaultVippsResponseFactory`, override the relevant methods and register it in your initialization module as your implementation of `IVippsResponseFactory`.

## Express workflows

There's slightly different workflows depending on where you implement Vipps Express. Here's an overview of the preferred way the different implementations could work.

### Product page flow

- User clicks "Vipps Hurtigkasse" button on product page
- A cart with a different cart name then your default cart name is created and product is added to cart (to persist customers original cart)
- A flag with "VippsPaymentType" is saved on cart
- Initiate call to the Vipps API
- User gets redirected to the Vipps landing page and enters their phone number (if using a phone the Vipps app will be automatically opened instead)
- User opens the app
- A call to the shipping details endpoint is made and the available shipping methods and prices are returned
- User chooses shipping method and confirms the payment
- A callback is made to the callback endpoint and the cart is populated with the users information and a PurchaseOrder is created
- User gets redirected to fallback controller

### Cart page flow

- User clicks "Vipps Hurtigkasse" button on product page
- A flag with "VippsPaymentType" is saved on cart
- Initiate call to the Vipps API
- User gets redirected to the Vipps landing page and enters their phone number (if using a phone the Vipps app will be automatically opened instead)
- User opens the app
- A call to the shipping details endpoint is made and the available shipping methods and prices are returned
- User chooses shipping method and confirms the payment
- A callback is made to the callback endpoint and the cart is populated with the users information and a PurchaseOrder is created
- User gets redirected to fallback controller

### Wish list page flow

- User clicks "Vipps Hurtigkasse" button on product page
- The customers WishList cart is loaded
- A cart with a different cart name then your default cart name is created and all products from wishlist are added (to persist customers original/wishlist cart)
- A flag with "VippsPaymentType" is saved on cart
- Initiate call to the Vipps API
- User gets redirected to the Vipps landing page and enters their phone number (if using a phone the Vipps app will be automatically opened instead)
- User opens the app
- A call to the shipping details endpoint is made and the available shipping methods and prices are returned
- User chooses shipping method and confirms the payment
- A callback is made to the callback endpoint and the cart is populated with the users information and a PurchaseOrder is created
- User gets redirected to fallback controller

