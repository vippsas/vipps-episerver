using System;
using System.Collections.Generic;
using System.Linq;
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
        private static Dictionary<string, IVippsApi> _marketApiDictionary = new Dictionary<string, IVippsApi>();
        private VippsConfiguration _configuration;
        private static IList<AuthenticationResponse> _authenticationResponses = new List<AuthenticationResponse>();
        private readonly ILogger _logger = LogManager.GetLogger(typeof(VippsServiceApiFactory));

        public IVippsApi Create(VippsConfiguration configuration)
        {
            var vippsApi = _marketApiDictionary.FirstOrDefault(x => x.Key == configuration.MarketId).Value;
            if (vippsApi == null)
            {
                _configuration = configuration;
                var client = new HttpClient(new AuthenticatedHttpClientHandler(GetToken))
                {
                    BaseAddress = new Uri(_configuration.ApiUrl)
                };

                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _configuration.SubscriptionKey);

                vippsApi = RestService.For<IVippsApi>(client);
                _marketApiDictionary.Add(configuration.MarketId, vippsApi);
            }

            return vippsApi;
        }

        private async Task<AuthenticationResponse> GetToken()
        {
            var existingResponse = _authenticationResponses.FirstOrDefault(x => x.MarketId == _configuration.MarketId &&  !x.IsExpired());

            if (existingResponse != null)
            {
                return existingResponse;
            }

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
                authenticationResponse.MarketId = _configuration.MarketId;
                _authenticationResponses.Remove(
                    _authenticationResponses.FirstOrDefault(x => x.MarketId == _configuration.MarketId));
                _authenticationResponses.Add(authenticationResponse);
            }

            catch (Exception ex)
            {
                _logger.Error($"Error getting vipps access token. Exception: {ex.Message} {ex.StackTrace}");
            }

            return authenticationResponse;
        }
    }
}
