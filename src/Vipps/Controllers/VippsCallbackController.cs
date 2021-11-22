using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using EPiServer.Logging;
using Vipps.Services;
using Vipps.Models.RequestModels;
using Vipps.Models.ResponseModels;
using System.Net;

namespace Vipps.Controllers
{
    [RoutePrefix("vippscallback")]
    public class VippsCallbackController : ApiController
    {
        private readonly IVippsResponseFactory _responseFactory;
        private readonly IVippsOrderSynchronizer _vippsOrderSynchronizer;
        private readonly HttpClient _client;
        private readonly string _cookieName;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(VippsCallbackController));

        public VippsCallbackController(
            IVippsResponseFactory responseFactory,
            IVippsOrderSynchronizer vippsOrderSynchronizer)
        {
            _responseFactory = responseFactory;
            _vippsOrderSynchronizer = vippsOrderSynchronizer;
            _cookieName = ConfigurationManager.AppSettings["Vipps:InstanceCookieName"] ?? "ARRAffinity";
            _client = new HttpClient(new HttpClientHandler {UseCookies = false});
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
                var requestInstanceId = GetInstanceId();
                var currentInstanceId = _vippsOrderSynchronizer.GetInstanceId();

                _logger.Information($"Request instance id is: {requestInstanceId}. Current instance id id: {currentInstanceId}. Order id: {orderId}");

                if (!string.IsNullOrEmpty(currentInstanceId))
                {
                    var response = GetCookieRedirectResponse(currentInstanceId);
                    return ResponseMessage(response);
                }

                HttpStatusCode result;

                if (paymentCallback.ShippingDetails != null && paymentCallback.UserDetails != null)
                {
                    _logger.Information($"Handling express callback for {orderId}");

                    result = await _responseFactory.HandleExpressCallback(orderId, contactId, marketId, cartName, paymentCallback);
                }
                else
                {
                    _logger.Information($"Handling checkout callback for {orderId}");

                    result = await _responseFactory.HandleCallback(orderId, contactId, marketId, cartName, paymentCallback);
                }

                return Content(result, string.Empty);
            }

            catch (Exception ex)
            {
                _logger.Error($"{ex.Message} {ex.StackTrace}");
                return InternalServerError(ex);
            }
        }

        private string GetInstanceId()
        {
            return GetCookieValue(_cookieName);
        }

        private string GetCookieValue(string cookieName)
        {
            var cookie = Request.Headers.GetCookies(cookieName).FirstOrDefault();

            if (cookie == null)
                return null;

            return cookie[cookieName].Value;
        }

        private HttpResponseMessage GetCookieRedirectResponse(string instanceId)
        {
            var message = new HttpResponseMessage(HttpStatusCode.Found);
            var cookies = new []
            {
                new CookieHeaderValue(_cookieName, instanceId)
            };

            try
            {
                message.Headers.Location = Request.RequestUri;
                message.Headers.AddCookies(cookies);
                message.RequestMessage = Request;
                return message;
            }
            catch
            {
                message.Dispose();
                throw;
            }
        }
    }
}