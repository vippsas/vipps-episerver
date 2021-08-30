using System;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Plugins.Payment;
using Vipps.Services;

namespace Vipps
{
    public class VippsPaymentGateway : AbstractPaymentGateway, IPaymentPlugin
    {
        internal Injected<IVippsPaymentService> InjectedVippsPaymentsService { get; set; }
        private IVippsPaymentService VippsPaymentsService => InjectedVippsPaymentsService.Service;

        public override bool ProcessPayment(Payment payment, ref string message)
        {
            var orderGroup = payment.Parent.Parent;
            var result = ProcessPayment(orderGroup, payment);
            message = result.Message;
            return result.IsSuccessful;
        }

        public PaymentProcessingResult ProcessPayment(IOrderGroup orderGroup, IPayment payment)
        {
            var shipment = orderGroup.GetFirstShipment();
            return ProcessPayment(orderGroup, payment, shipment);
        }

        public PaymentProcessingResult ProcessPayment(IOrderGroup orderGroup, IPayment payment, IShipment shipment)
        {
            switch (payment.TransactionType)
            {
                case nameof(TransactionType.Authorization):
                    return VippsPaymentsService.Initiate(orderGroup, payment);

                case nameof(TransactionType.Capture):
                    return VippsPaymentsService.Capture(orderGroup, payment);

                case nameof(TransactionType.Sale):
                    return VippsPaymentsService.Initiate(orderGroup, payment);

                case nameof(TransactionType.Void):
                    return VippsPaymentsService.Cancel(orderGroup, payment);

                case nameof(TransactionType.Credit):
                    return VippsPaymentsService.Refund(orderGroup, payment);

                default:
                    throw new NotImplementedException();
            }

        }
    }
}
