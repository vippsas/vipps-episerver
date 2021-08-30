# Checklist

There are some checks you should do before going live, to make sure that a payment can be created in all possible scenarios

 - [ ] FallbackController
  - Set Vipps:PollingInterval in app settings to a high ms count (f.ex. 600000, so polling runs every 10 minutes)
  - Set Site Base Url in Commerce Manager to something that isn't correct
  - Make sure your Fallback Url is correct in Commerce Manager
  - Go through the order flow and comfirm an order has been created
  
  This forces the order to be created when the user is redirected to your fallback controller. (In the ProcessAuthorizationAsync method in VippsPaymentService).
  
  
 - [ ] Callbacks
  - Comment out your [polling initialization](configure.md#polling)
  - Set Fallback Url in Commerce Manager to something that isn't correct
  - Make sure your Site Base Url is correct in Commerce Manager (your ngrok generated url if testing locally)
  - Go through the order flow and comfirm an order has been created
  
  This forces the order to be created in the api callback from Vipps.
  
  
  - [ ] Polling
  - Set Vipps:PollingInterval in app settings to what you're need is or remove the app setting (Default value 2000ms)
  - Set Fallback Url and Site Base Url in Commerce Manager to something that isn't correct
  - Go through the order flow and comfirm an order has been created
  
  This forces the order to be created through the built in polling against the Vipps api.