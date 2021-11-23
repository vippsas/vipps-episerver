using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using EPiServer.Logging;
using Vipps.Services;
using Vipps.Models.RequestModels;
using Vipps.Models.ResponseModels;
using System.Net;
using Newtonsoft.Json;
using System.Text;

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
            _client = new HttpClient(new HttpClientHandler { UseCookies = false });
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
                var requestInstanceId = GetRequestInstanceId();
                var currentInstanceId = _vippsOrderSynchronizer.GetInstanceId();

                _logger.Information($"Request instance id is: '{requestInstanceId}'. Current instance id id: '{currentInstanceId}'. Order id: '{orderId}'");

                ValidateCookieIfExists(currentInstanceId);

                if (!string.IsNullOrEmpty(currentInstanceId) && requestInstanceId != currentInstanceId)
                {
                    var response = await ResendWithCookie(currentInstanceId, paymentCallback).ConfigureAwait(false);
                    return ResponseMessage(response);
                }

                HttpStatusCode result;

                if (paymentCallback.ShippingDetails != null && paymentCallback.UserDetails != null)
                {
                    _logger.Information($"Handling express callback for {orderId}");

                    result = await _responseFactory.HandleExpressCallback(orderId, contactId, marketId, cartName, paymentCallback)
                                                   .ConfigureAwait(false);
                }
                else
                {
                    _logger.Information($"Handling checkout callback for {orderId}");

                    result = await _responseFactory.HandleCallback(orderId, contactId, marketId, cartName, paymentCallback)
                                                   .ConfigureAwait(false);
                }

                return Content(result, string.Empty);
            }

            catch (Exception ex)
            {
                _logger.Error($"{ex.Message} {ex.StackTrace}");
                return InternalServerError(ex);
            }
        }

        private string GetRequestInstanceId()
        {
            return Request.Headers.Authorization?.ToString();
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

        private async Task<HttpResponseMessage> ResendWithCookie(string instanceId, PaymentCallback paymentCallback)
        {
            _logger.Information($"Resending post request for order {paymentCallback.OrderId} to instance {instanceId} to url {Request.RequestUri}");

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = Request.RequestUri,
                Headers = 
                {
                    { nameof(HttpRequestHeader.Authorization), instanceId },
                    { nameof(HttpRequestHeader.Cookie), $"{_cookieName}={instanceId}" }
                },                
                Content = new StringContent(JsonConvert.SerializeObject(paymentCallback), Encoding.UTF8, "application/json")
            };

            return await _client.SendAsync(httpRequestMessage)
                                .ConfigureAwait(false);
        }
    }
}