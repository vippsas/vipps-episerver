using System;
using System.Threading.Tasks;
using Vipps.Models;

namespace Vipps.Services
{
    public interface IVippsAsyncPaymentService
    {
        Task<ProcessAuthorizationResponse> ProcessAuthorization(Guid contactId, string marketId, string cartName,
            string orderId);
    }
}