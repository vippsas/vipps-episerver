using System;
using System.Net.Http;
using System.Threading.Tasks;
using EPiServer.Logging;
using Newtonsoft.Json;
using Refit;
using Vipps.Models.ResponseModels;

namespace Vipps
{
    public class VippsServiceApiFactory
    {
        private static IVippsApi _vippsApi;
        private VippsConfiguration _configuration;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(VippsServiceApiFactory));

        public IVippsApi Create(VippsConfiguration configuration)
        {
            if (_vippsApi == null)
            {
                _configuration = configuration;
                var client = new HttpClient(new AuthenticatedHttpClientHandler(GetToken))
                {
                    BaseAddress = new Uri(_configuration.ApiUrl)
                };

                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _configuration.SubscriptionKey);

                _vippsApi = RestService.For<IVippsApi>(client);
            }

            return _vippsApi;
        }

        private async Task<AuthenticationResponse> GetToken()
        {
            var authenticationResponse = new AuthenticationResponse();
            var client = new HttpClient
            {
                BaseAddress = new Uri(_configuration.ApiUrl)
            };

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("client_id", _configuration.ClientId);
            client.DefaultRequestHeaders.Add("client_secret", _configuration.ClientSecret);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _configuration.SubscriptionKey);
            client.Timeout = TimeSpan.FromSeconds(5);
            try
            {
                var response = await client.PostAsync("/accessToken/get", null).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                authenticationResponse =
                    JsonConvert.DeserializeObject<AuthenticationResponse>(await response.Content.ReadAsStringAsync());
            }

            catch (Exception ex)
            {
                _logger.Error($"Error getting vipps access token. Exception: {ex.Message} {ex.StackTrace}");
            }

            return authenticationResponse;
        }
    }
}
