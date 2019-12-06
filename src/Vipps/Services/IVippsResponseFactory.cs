using System.Net;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using Vipps.Models.RequestModels;
using Vipps.Models.ResponseModels;

namespace Vipps.Services
{
    public interface IVippsResponseFactory
    {
        Task<HttpStatusCode> HandleCallback(ICart cart, PaymentCallback paymentCallback);
        Task<HttpStatusCode> HandleExpressCallback(ICart cart, PaymentCallback paymentCallback);
        ShippingDetailsResponse GetShippingDetails(string orderId, string contactId, string marketId, ShippingRequest shippingRequest);
    }
}