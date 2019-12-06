namespace Vipps.Models
{
    public enum VippsCallbackStatus
    {
        RESERVED,
        SALE,
        RESERVE_FAILED,
        SALE_FAILED,
        CANCELLED,
        REJECTED
    }

    public enum VippsExpressCallbackStatus
    {
        RESERVE,
        SALE,
        CANCELLED,
        REJECTED
    }

    public enum VippsDetailsResponseStatus
    {
        INITIATE,
        RESERVE,
        SALE,
        CAPTURE,
        REFUND,
        CANCEL,
        VOID
    }

    public enum VippsStatusResponseStatus
    {
        INITIATE,
        REGISTER,
        RESERVE,
        SALE,
        CAPTURE,
        REFUND,
        CANCEL,
        VOID,
        FAILED,
        REJECTED
    }

    public enum VippsUpdatePaymentResponseStatus
    {
        Initiate,
        Captured,
        Cancelled,
        Refund
    }
}
