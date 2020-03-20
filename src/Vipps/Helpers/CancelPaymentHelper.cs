using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;

namespace Vipps.Helpers
{
    public static class CancelPaymentHelper
    {
        private static Injected<IOrderGroupFactory> _orderGroupFactory;
        private static Injected<IPaymentProcessor> _paymentProcessor;

        public static void CancelPayment(IOrderGroup orderGroup, IPayment previousPayment)
        {
            var voidPayment = orderGroup.CreatePayment(_orderGroupFactory.Service);
            voidPayment.PaymentType = previousPayment.PaymentType;
            voidPayment.PaymentMethodId = previousPayment.PaymentMethodId;
            voidPayment.PaymentMethodName = previousPayment.PaymentMethodName;
            voidPayment.Amount = previousPayment.Amount;
            voidPayment.Status = PaymentStatus.Pending.ToString();
            voidPayment.TransactionType = TransactionType.Void.ToString();

            orderGroup.AddPayment(voidPayment);

            _paymentProcessor.Service.ProcessPayment(orderGroup, voidPayment, orderGroup.GetFirstShipment());
        }
    }
}