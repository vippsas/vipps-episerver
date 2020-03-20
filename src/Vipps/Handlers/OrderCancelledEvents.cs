using System;
using System.Linq;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Orders;
using Vipps.Extensions;
using Vipps.Helpers;

namespace Vipps.Handlers
{
    public class OrderCancelledEvent
    {
        public OrderCancelledEvent(IPurchaseOrder purchaseOrder)
        {
            PurchaseOrder = purchaseOrder ?? throw new ArgumentNullException(nameof(purchaseOrder));
        }

        public IPurchaseOrder PurchaseOrder { get; }
    }

    public class OrderCancelledEventHandler
    {
        private readonly IPaymentProcessor _paymentProcessor;
        private readonly IOrderGroupFactory _orderGroupFactory;
        private IPurchaseOrder _order;
        private IOrderForm _orderForm;

        public OrderCancelledEventHandler(
            IPaymentProcessor paymentProcessor,
            IOrderGroupFactory orderGroupFactory)
        {
            _paymentProcessor = paymentProcessor ?? throw new ArgumentNullException(nameof(paymentProcessor));
            _orderGroupFactory = orderGroupFactory ?? throw new ArgumentNullException(nameof(orderGroupFactory));
        }

        public void Handle(OrderCancelledEvent ev)
        {
            _order = ev.PurchaseOrder;
            _orderForm = _order.GetFirstForm();

            if (AlreadyVoided()) return;

            var previousPayment = _orderForm.Payments.FirstOrDefault(x => x.IsVippsPayment() && x.Status == PaymentStatus.Processed.ToString());
            if (previousPayment == null) return;

            PaymentHelper.CancelPayment(_order, previousPayment);
        }

        private bool AlreadyVoided()
        {
            return _orderForm.Payments.Any(
                p => p.TransactionType == TransactionType.Void.ToString()
                     || p.TransactionType == TransactionType.Capture.ToString());
        }
    }
}
