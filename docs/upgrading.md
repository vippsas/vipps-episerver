# Upgrading

## Automatic database migration
For performance reasons the VippsOrderId field has been migrated to use a different data type. Existing Vipps installs will undergo a migration that happens during site startup.
Make sure to _backup the commerce database_ before upgrading if any error or data loss should happen. Actions taken during this migration are logged on `DEBUG` level.

## Affected tables
The following tables are affected by the database migration, please ensure beforehand that no tables have constraints (like indexes) that prohibit altering the column `VippsOrderId`.

```
OrderGroup_PurchaseOrder
OrderGroup_PurchaseOrder_Localization
OrderGroup_ShoppingCart
OrderGroup_ShoppingCart_Localization
```

## Breaking changes

### VippsPaymentService and VippsAsyncPaymentService
Dependency reference to `IVippsService` has been removed from the constructor. 
This will result in a build error that can be resolved by removing the `IVippsService` parameter from any inheriting classes.

### IVippsOrderProcessor
`Task<ProcessOrderResponse> CreatePurchaseOrder(ICart cart)` has a new signature `ProcessOrderResponse CreatePurchaseOrder(ICart cart)` and is executed synchronously. 
If you previously used async method calls inside of an overriding class, please use `AsyncHelper.RunSync(() => ...)` included in the Vipps package to execute.

Due to some performance concerns we have made some adjustments to the default implementation of IVippsOrderProcessor which should resonate in any inheriting implementation.

`[Obsolete] Task<ProcessOrderResponse> ProcessOrderDetails(DetailsResponse detailsResponse, string orderId, Guid contactId, string marketId, string cartName)`
Is now considered Obsolete and will be removed in the future. Instead two different versions are to take its place.

`Task<ProcessOrderResponse> FetchAndProcessOrderDetails(string orderId, Guid contactId, string marketId, string cartName)`
Encapsulates all loading of `IPurchaseOrder`, `ICart` and `IVippsUserDetails` to have more control over simultaneous execution. This method should be used by callback controllers.

`Task<ProcessOrderResponse> ProcessOrderDetails(DetailsResponse detailsResponse, string orderId, ICart cart)`
Reuses an already loaded cart and executes without the need for loading a purchase order. This method is used internally by the polling logic and is not suited for public exposure.