using System;
using System.Threading.Tasks;
using System.Web.Http;
using EPiServer.Logging;
using Vipps.Services;
using Vipps.Models.RequestModels;
using Vipps.Models.ResponseModels;

namespace Vipps.Controllers
{
    [RoutePrefix("vippscallback")]
    public class VippsCallbackController : ApiController
    {
        private readonly IVippsService _vippsService;
        private readonly IVippsResponseFactory _responseFactory;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(VippsCallbackController));

        public VippsCallbackController(
            IVippsResponseFactory responseFactory,
            IVippsService vippsService)
        {
            _responseFactory = responseFactory;
            _vippsService = vippsService;
        }

        [Route("{contactId}/{marketId}/{cartName}/v2/payments/{orderId}/shippingDetails")]
        [AcceptVerbs("Post")]
        [HttpPost]
        public IHttpActionResult ShippingDetails(string contactId, string marketId, string cartName, string orderId, [FromBody]ShippingRequest shippingRequest)
        {
            try
            {
                var response = _responseFactory.GetShippingDetails(orderId, contactId, marketId, cartName, shippingRequest);
                return Ok(response);
            }

            catch (Exception ex)
            {
                _logger.Error($"{ex.Message} {ex.StackTrace}");
                return InternalServerError(ex);
            }
            
        }

        [Route("v2/consents/{userId}")]
        [AcceptVerbs("Delete")]
        [HttpDelete]
        public IHttpActionResult RemoveUserConsent(string userId)
        {
            return Ok();
        }

        [Route("{contactId}/{marketId}/{cartName}/v2/payments/{orderId}")]
        [AcceptVerbs("Post")]
        [HttpPost]
        public async Task<IHttpActionResult> Callback(string contactId, string marketId, string cartName, string orderId, [FromBody]PaymentCallback paymentCallback)
        {
            try
            {
                if (paymentCallback.ShippingDetails != null && paymentCallback.UserDetails != null)
                {
                    _logger.Information($"Handling express callback for {orderId}");
                    return Content(await _responseFactory.HandleExpressCallback(orderId, contactId, marketId, cartName, paymentCallback), string.Empty);
                }

                _logger.Information($"Handling checkout callback for {orderId}");
                return Content(await _responseFactory.HandleCallback(orderId, contactId, marketId, cartName, paymentCallback), string.Empty);

            }

            catch (Exception ex)
            {
                _logger.Error($"{ex.Message} {ex.StackTrace}");
                return InternalServerError(ex);
            }
        }
    }
}