using System;
using System.Threading.Tasks;
using Vipps.Helpers;
using Vipps.Models;

namespace Vipps.Services
{
    public class VippsAsyncPaymentService : VippsServiceBase, IVippsAsyncPaymentService
    {
        private readonly IVippsService _vippsService;
        private readonly IVippsOrderProcessor _vippsOrderProcessor;

        public VippsAsyncPaymentService(IVippsService vippsService, IVippsOrderProcessor vippsOrderProcessor)
        {
            _vippsService = vippsService;
            _vippsOrderProcessor = vippsOrderProcessor;
        }
        
        public async Task<ProcessAuthorizationResponse> ProcessAuthorization(Guid contactId, string marketId, string cartName, string orderId)
        {
            var purchaseOrder = _vippsService.GetPurchaseOrderByOrderId(orderId);
            if (purchaseOrder != null)
            {
                return new ProcessAuthorizationResponse
                {
                    PurchaseOrder = purchaseOrder,
                    Processed = true
                };
            }

            var cart = _vippsService.GetCartByContactId(contactId, marketId, cartName);
            var paymentType = PaymentTypeHelper.GetVippsPaymentType(cart);

            var orderDetails = await _vippsService.GetOrderDetailsAsync(orderId, marketId);
            var result = await _vippsOrderProcessor.ProcessOrderDetails(orderDetails, orderId, contactId, marketId, cartName);
            if (result.PurchaseOrder != null)
            {
                return new ProcessAuthorizationResponse(result)
                {
                    Processed = true
                };
            }

            return new ProcessAuthorizationResponse(result)
            {
                PaymentType = paymentType,
                Processed = false
            };
        }
    }
}