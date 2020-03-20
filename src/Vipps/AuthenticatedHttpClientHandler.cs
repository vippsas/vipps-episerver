using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Vipps.Models.ResponseModels;

namespace Vipps
{
    /// <inheritdoc />
    /// <summary>
    /// See https://github.com/paulcbetts/refit#authorization-dynamic-headers-redux
    /// </summary>
    public class AuthenticatedHttpClientHandler : HttpClientHandler
    {
        private readonly Func<Task<AuthenticationResponse>> _getToken;
        protected AuthenticationResponse _authenticationResponse;

        public AuthenticatedHttpClientHandler(Func<Task<AuthenticationResponse>> getToken)
        {
            _getToken = getToken ?? throw new ArgumentNullException(nameof(getToken));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_authenticationResponse == null || _authenticationResponse.IsExpired())
            {
                _authenticationResponse = await _getToken().ConfigureAwait(false);
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", $"{_authenticationResponse.AccesToken}");
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
