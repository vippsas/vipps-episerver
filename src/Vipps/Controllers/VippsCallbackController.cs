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
                var authorization = Request.Headers.Authorization?.ToString();
                var currentInstanceId = _vippsOrderSynchronizer.GetInstanceId();
                _logger.Information($"Request instance id is: {authorization}. Current instance id id: {currentInstanceId}. Order id: {orderId}");

                ValidateCookieIfExists(currentInstanceId);

                if (string.IsNullOrEmpty(currentInstanceId) || currentInstanceId == authorization)
                {
                    if (paymentCallback.ShippingDetails != null && paymentCallback.UserDetails != null)
                    {
                        _logger.Information($"Handling express callback for {orderId}");
                        return Content(await _responseFactory.HandleExpressCallback(orderId, contactId, marketId, cartName, paymentCallback), string.Empty);
                    }

                    _logger.Information($"Handling checkout callback for {orderId}");
                    return Content(await _responseFactory.HandleCallback(orderId, contactId, marketId, cartName, paymentCallback), string.Empty);
                }

                ResendWithCookie(authorization, paymentCallback);
                return Ok();
            }

            catch (Exception ex)
            {
                _logger.Error($"{ex.Message} {ex.StackTrace}");
                return InternalServerError(ex);
            }
        }

        private void ValidateCookieIfExists(string currentInstanceId)
        {
            var cookie = Request.Headers.GetCookies(_cookieName).FirstOrDefault();
            if (cookie == null)
            {
                //Request from vipps
                return;
            }

            var value = cookie[_cookieName].Value;
            if (value != currentInstanceId)
            {
                //We set the cookie. Failed to redirect to instance
                throw new Exception($"Cookie value {value} not equal to current instance id {currentInstanceId}.");
            }
        }

        private void ResendWithCookie(string instanceId, PaymentCallback paymentCallback)
        {
            _logger.Information($"Resending post request for order {paymentCallback.OrderId} to instance {instanceId}");
            var url = Request.RequestUri;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(instanceId);
            _client.DefaultRequestHeaders.Add("Cookie", $"{_cookieName}={instanceId}");
            _client.PostAsJsonAsync(url, paymentCallback);
        }
    }
}