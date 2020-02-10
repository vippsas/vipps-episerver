using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Managers;
using Vipps.Extensions;
using Vipps.Helpers;
using Vipps.Models;
using Vipps.Models.Partials;
using Vipps.Models.RequestModels;
using Vipps.Models.ResponseModels;

namespace Vipps.Services
{
    public class DefaultVippsResponseFactory : IVippsResponseFactory
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPromotionEngine _promotionEngine;
        private readonly IOrderGroupFactory _orderGroupFactory;
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IVippsOrderCreator _vippsOrderCreator;
        private readonly IVippsService _vippsService;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(DefaultVippsResponseFactory));

        public DefaultVippsResponseFactory(
            IOrderRepository orderRepository,
            IPromotionEngine promotionEngine,
            IOrderGroupFactory orderGroupFactory,
            IOrderGroupCalculator orderGroupCalculator,
            IVippsOrderCreator vippsOrderCreator,
            IVippsService vippsService)
        {
            _orderRepository = orderRepository;
            _promotionEngine = promotionEngine;
            _orderGroupFactory = orderGroupFactory;
            _orderGroupCalculator = orderGroupCalculator;
            _vippsOrderCreator = vippsOrderCreator;
            _vippsService = vippsService;
        }

        public virtual async Task<HttpStatusCode> HandleCallback(ICart cart, PaymentCallback paymentCallback)
        {
            var payment = cart.GetFirstForm().Payments.FirstOrDefault(x => x.IsVippsPayment());
            if (payment != null)
            {
                if (paymentCallback.TransactionInfo.Status == VippsCallbackStatus.RESERVED.ToString() ||
                    paymentCallback.TransactionInfo.Status == VippsCallbackStatus.SALE.ToString())
                {
                    payment.Status = PaymentStatus.Processed.ToString();
                    OrderNoteHelper.AddNoteAndSaveChanges(cart, payment, "Initiate",
                        $"Payment with order id: {paymentCallback.OrderId} successfully initiated with {paymentCallback.TransactionInfo?.Status}");

                    var result = await _vippsOrderCreator.LoadOrCreatePurchaseOrder(cart, paymentCallback.OrderId);
                    if (!result.PurchaseOrderCreated)
                    {
                        CancelPaymentHelper.CancelPayment(cart, payment);
                    }
                }
                else
                {
                    payment.Status = PaymentStatus.Failed.ToString();
                    OrderNoteHelper.AddNoteAndSaveChanges(cart, payment, "Initiate",
                        $"Payment with order id: {paymentCallback.OrderId} failed to initiate. Status: {paymentCallback.TransactionInfo?.Status}");
                }

                return HttpStatusCode.OK;
            }

            _logger.Warning($"No vipps payment found for vipps order id {paymentCallback.OrderId}");
            return HttpStatusCode.BadRequest;
        }

        public virtual async Task<HttpStatusCode> HandleExpressCallback(ICart cart, PaymentCallback paymentCallback)
        {
            var payment = cart.GetFirstForm().Payments.FirstOrDefault(x => x.IsVippsPayment());
            if (payment != null)
            {
                if (paymentCallback.TransactionInfo.Status == VippsExpressCallbackStatus.RESERVE.ToString() ||
                    paymentCallback.TransactionInfo.Status == VippsExpressCallbackStatus.SALE.ToString())
                {
                    var orderAddress = AddressHelper.UserDetailsAndShippingDetailsToOrderAddress(paymentCallback.UserDetails, paymentCallback.ShippingDetails, cart);

                    UpdateShipment(cart, orderAddress, paymentCallback.ShippingDetails);
                    UpdatePayment(cart, payment, orderAddress, paymentCallback.TransactionInfo);

                    cart.ApplyDiscounts(_promotionEngine, new PromotionEngineSettings());
                    OrderNoteHelper.AddNoteAndSaveChanges(cart, payment, "Initiate",
                        $"Payment with order id: {paymentCallback.OrderId} successfully initiated with {paymentCallback.TransactionInfo?.Status}");

                    var result = await _vippsOrderCreator.LoadOrCreatePurchaseOrder(cart, paymentCallback.OrderId);
                    if (!result.PurchaseOrderCreated)
                    {
                        CancelPaymentHelper.CancelPayment(cart, payment);
                    }
                }

                else
                {

                    payment.Status = PaymentStatus.Failed.ToString();
                    OrderNoteHelper.AddNoteAndSaveChanges(cart, payment, "Initiate",
                        $"Payment with order id: {paymentCallback.OrderId} failed to initiate. Status: {paymentCallback.TransactionInfo?.Status}");
                }

                return HttpStatusCode.OK;
            }

            _logger.Warning($"No vipps payment found for vipps order id {paymentCallback.OrderId}");
            return HttpStatusCode.BadRequest;
        }

        public virtual ShippingDetailsResponse GetShippingDetails(string orderId, string contactId, string marketId, ShippingRequest shippingRequest)
        {
            var cart = _vippsService.GetCartByContactId(contactId, marketId, orderId);

            var shippingMethods = ShippingManager.GetShippingMethodsByMarket(cart.MarketId.Value, false).ShippingMethod.ToList().OrderBy(x => x.Ordering);

            var shippingDetails = new List<ShippingDetail>();

            var counter = 1;

            foreach (var shippingMethod in shippingMethods)
            {
                lock (cart)
                {
                    var shipment = cart.GetFirstShipment();
                    shipment.ShippingAddress = AddressHelper.ShippingRequestToOrderAddress(shippingRequest, cart);
                    shipment.ShippingMethodId = shippingMethod.ShippingMethodId;
                    cart.ApplyDiscounts();

                    shippingDetails.Add(new ShippingDetail
                    {
                        ShippingMethodId = shippingMethod.ShippingMethodId.ToString(),
                        ShippingCost = Convert.ToDouble(cart.GetShippingTotal().Amount),
                        ShippingMethod = shippingMethod.DisplayName,
                        Priority = shippingMethod.IsDefault ? 0 : counter++
                    });
                }
            }

            return new ShippingDetailsResponse
            {
                OrderId = orderId,
                ShippingDetails = shippingDetails
            };
        }

        private static void UpdateShipment(ICart cart, IOrderAddress orderAddress, ShippingDetail shippingDetails)
        {
            var shipment = cart.GetFirstShipment();
            shipment.ShippingMethodId = new Guid(shippingDetails.ShippingMethodId);
            shipment.ShippingAddress = orderAddress;
        }

        private void UpdatePayment(ICart cart, IPayment payment, IOrderAddress orderAddress, TransactionInfo transactionInfo)
        {
            cart.GetFirstForm().Payments.Clear();
            var total = cart.GetTotal(_orderGroupCalculator);
            var newPayment = PaymentHelper.CreateVippsPayment(cart, total, payment.PaymentMethodId);
            newPayment.Status = PaymentStatus.Processed.ToString();
            newPayment.TransactionID = transactionInfo.TransactionId;
            cart.AddPayment(newPayment, _orderGroupFactory);
            newPayment.BillingAddress = orderAddress;
        }
    }
}
