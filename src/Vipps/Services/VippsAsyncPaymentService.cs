using System;
using System.Threading.Tasks;
using Vipps.Models;

namespace Vipps.Services
{
    public class VippsAsyncPaymentService : VippsServiceBase, IVippsAsyncPaymentService
    {
        private readonly IVippsOrderProcessor _vippsOrderProcessor;

        public VippsAsyncPaymentService(IVippsOrderProcessor vippsOrderProcessor)
        {
            _vippsOrderProcessor = vippsOrderProcessor;
        }
        
        public async Task<ProcessAuthorizationResponse> ProcessAuthorization(Guid contactId, string marketId, string cartName, string orderId)
        {
            var result = await _vippsOrderProcessor.FetchAndProcessOrderDetailsAsync(orderId, contactId, marketId, cartName);
            if (result.PurchaseOrder != null)
            {
                return new ProcessAuthorizationResponse(result)
                {
                    Processed = true
                };
            }

            return new ProcessAuthorizationResponse(result)
            {
                Processed = false
            };
        }
    }
}