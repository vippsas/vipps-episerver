<!-- START_METADATA
---
title: Optimizely Checklist
sidebar_label: Checklist
sidebar_position: 40
pagination_next: null
pagination_prev: null
---
END_METADATA -->

# Checklist

There are some checks you should do before going live, to make sure that a payment can be created in all possible scenarios.

 - [ ] *FallbackController*
    - Comment out your [polling initialization](configure.md#polling).
    - Set *Site Base Url* in Commerce Manager to something that isn't correct.
    - Make sure your *Fallback Url* is correct in the *Commerce Manager*.
    - Go through the order flow and confirm that an order has been created.

  This forces the order to be created when the user is redirected to your fallback controller. (In the ProcessAuthorizationAsync method in VippsPaymentService).

 - [ ] Callbacks
    - Comment out your [polling initialization](configure.md#polling).
    - *Set Fallback Url* in Commerce Manager to something that is not correct.
    - Make sure your *Site Base Url* is correct in *Commerce Manager* (your ngrok-generated url, if testing locally).
    - Go through the order flow and confirm an order has been created.

  This forces the order to be created in the API callback from Vipps.

  - [ ] Polling
    - Set *Fallback Url* and *Site Base Url* in *Commerce Manager* to something that is not correct.
    - Go through the order flow and confirm that an order has been created.

This forces the order to be created through the built-in polling against the Vipps API.
