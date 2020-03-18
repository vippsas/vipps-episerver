using System;
using System.Linq;
using EPiServer.Logging;
using Newtonsoft.Json;
using Refit;
using Vipps.Models.ResponseModels;

namespace Vipps.Services
{
    public class VippsServiceBase
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(VippsServiceBase));
        protected string GetErrorMessage(ApiException apiException)
        {
            if (apiException.HasContent)
            {
                try
                {
                    var errorResponses = JsonConvert.DeserializeObject<ErrorResponse[]>(apiException.Content);
                    var errorMessage = string.Join(". ",
                        errorResponses.Where(x => !string.IsNullOrEmpty(x?.ErrorMessage)).Select(x => x.ErrorMessage));

                    return errorMessage;
                }

                catch (Exception ex)
                {
                    _logger.Log(Level.Warning, $"Error deserializing error message. Content: {apiException?.Content}. Exception: {ex.Message}");
                }
            }

            return apiException.Message;
        }
    }
}
