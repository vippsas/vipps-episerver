using System;
using System.Threading.Tasks;
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
                    return VippsPaymentsService.InitiateAsync(orderGroup, payment).GetAwaiter().GetResult();

                case nameof(TransactionType.Capture):
                    return VippsPaymentsService.CaptureAsync(orderGroup, payment, shipment).GetAwaiter().GetResult();

                case nameof(TransactionType.Sale):
                    return VippsPaymentsService.InitiateAsync(orderGroup, payment).GetAwaiter().GetResult();

                case nameof(TransactionType.Void):
                    return VippsPaymentsService.CancelAsync(orderGroup, payment, shipment).GetAwaiter().GetResult();

                case nameof(TransactionType.Credit):
                    return VippsPaymentsService.RefundAsync(orderGroup, payment, shipment).GetAwaiter().GetResult();

                default:
                    throw new NotImplementedException();
            }

        }
    }
}
