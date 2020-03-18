using System.Net;
using System.Threading.Tasks;
using Vipps.Models.RequestModels;
using Vipps.Models.ResponseModels;

namespace Vipps.Services
{
    public interface IVippsResponseFactory
    {
        Task<HttpStatusCode> HandleCallback(string orderId, string contactId, string marketId, string cartName, PaymentCallback paymentCallback);
        Task<HttpStatusCode> HandleExpressCallback(string orderId, string contactId, string marketId, string cartName, PaymentCallback paymentCallback);
        ShippingDetailsResponse GetShippingDetails(string orderId, string contactId, string marketId, string cartName, ShippingRequest shippingRequest);
    }
}