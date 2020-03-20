using System.Threading.Tasks;
using Refit;
using Vipps.Models.RequestModels;
using Vipps.Models.ResponseModels;

namespace Vipps
{
    public interface IVippsApi
    {
        [Post("/ecomm/v2/payments")]
        Task<InitiatePaymentResponse> Initiate([Body]InitiatePaymentRequest initiatePaymentRequest);

        [Post("/ecomm/v2/payments/{orderId}/capture")]
        Task<UpdatePaymentResponse> Capture([AliasAs("orderId")]string orderId, [Body]UpdatePaymentRequest capturePaymentRequest);

        [Put("/ecomm/v2/payments/{orderId}/cancel")]
        Task<UpdatePaymentResponse> Cancel([AliasAs("orderId")]string orderId, [Body]UpdatePaymentRequest capturePaymentRequest);

        [Post("/ecomm/v2/payments/{orderId}/refund")]
        Task<UpdatePaymentResponse> Refund([AliasAs("orderId")]string orderId, [Body]UpdatePaymentRequest capturePaymentRequest);
        
        [Get("/ecomm/v2/payments/{orderId}/status")]
        Task<StatusResponse> Status([AliasAs("orderId")] string orderId);

        [Get("/ecomm/v2/payments/{orderId}/details")]
        Task<DetailsResponse> Details([AliasAs("orderId")] string orderId);
    }
}
