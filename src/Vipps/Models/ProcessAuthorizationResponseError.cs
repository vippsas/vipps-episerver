namespace Vipps.Models
{
    public class ProcessAuthorizationResponseError
    {
        public string ErrorMessage { get; set; }
        public ProcessAuthorizationErrorType ProcessAuthorizationErrorType { get; set; }
        public VippsStatusResponseStatus VippsStatusResponseStatus { get; set; }
    }
}