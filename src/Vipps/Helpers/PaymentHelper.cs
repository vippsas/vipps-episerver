using System;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.Globalization;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;

namespace Vipps.Helpers
{
    public static class PaymentHelper
    {
        private static Injected<IOrderGroupFactory> _orderGroupFactory;
        private static Injected<IPaymentProcessor> _paymentProcessor;
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(PaymentHelper));

        public static PaymentMethodDto GetVippsPaymentMethodDto()
        {
            return PaymentManager.GetPaymentMethodBySystemName(VippsConstants.VippsSystemKeyword,
                ContentLanguage.PreferredCulture.Name);
        }

        public static IPayment CreateVippsPayment(ICart cart, decimal amount, Guid vippsPaymentMethodId)
        {
            var payment = cart.CreatePayment(_orderGroupFactory.Service);
            payment.PaymentType = PaymentType.Other;
            payment.PaymentMethodId = vippsPaymentMethodId;
            payment.PaymentMethodName = VippsConstants.VippsSystemKeyword;
            payment.Amount = amount;
            payment.Status = PaymentStatus.Pending.ToString();
            payment.TransactionType = TransactionType.Authorization.ToString();

            return payment;
        }

        public static void CancelPayment(IOrderGroup orderGroup, IPayment previousPayment)
        {
            var voidPayment = orderGroup.CreatePayment(_orderGroupFactory.Service);
            voidPayment.PaymentType = previousPayment.PaymentType;
            voidPayment.PaymentMethodId = previousPayment.PaymentMethodId;
            voidPayment.PaymentMethodName = previousPayment.PaymentMethodName;
            voidPayment.Amount = previousPayment.Amount;
            voidPayment.Status = PaymentStatus.Pending.ToString();
            voidPayment.TransactionType = TransactionType.Void.ToString();
            voidPayment.TransactionID = previousPayment.TransactionID;

            orderGroup.AddPayment(voidPayment);

            _paymentProcessor.Service.ProcessPayment(orderGroup, voidPayment, orderGroup.GetFirstShipment());
        }

        public static void CancelPayment(ICart cart, int amount, string orderId)
        {
            var vippsPaymentMethodDto = GetVippsPaymentMethodDto().PaymentMethod.FirstOrDefault();
            if (vippsPaymentMethodDto == null)
            {
                _logger.Warning("No payment method is setup for vipps");
                return;
            }

            var payment = CreateVippsPayment(cart, amount.FormatAmountFromVipps(), vippsPaymentMethodDto.PaymentMethodId);
            payment.TransactionID = orderId;
            CancelPayment(cart, payment);
        }
    }
}
