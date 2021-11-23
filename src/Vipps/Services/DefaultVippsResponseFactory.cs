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
using Vipps.Helpers;
using Vipps.Models.Partials;
using Vipps.Models.RequestModels;
using Vipps.Models.ResponseModels;

namespace Vipps.Services
{
    public class DefaultVippsResponseFactory : IVippsResponseFactory
    {
        private readonly IPromotionEngine _promotionEngine;
        private readonly IOrderGroupFactory _orderGroupFactory;
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IVippsOrderProcessor _vippsOrderCreator;
        private readonly IVippsService _vippsService;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(DefaultVippsResponseFactory));

        public DefaultVippsResponseFactory(
            IPromotionEngine promotionEngine,
            IOrderGroupFactory orderGroupFactory,
            IOrderGroupCalculator orderGroupCalculator,
            IVippsOrderProcessor vippsOrderCreator,
            IVippsService vippsService)
        {
            _promotionEngine = promotionEngine;
            _orderGroupFactory = orderGroupFactory;
            _orderGroupCalculator = orderGroupCalculator;
            _vippsOrderCreator = vippsOrderCreator;
            _vippsService = vippsService;
        }
        
        public virtual async Task<HttpStatusCode> HandleCallback(string orderId, string contactId, string marketId, string cartName, PaymentCallback paymentCallback)
        {
            await _vippsOrderCreator.ProcessPaymentCallback(paymentCallback, orderId, contactId, marketId, cartName)
                                    .ConfigureAwait(false);

            return HttpStatusCode.OK;
        }

        public virtual async Task<HttpStatusCode> HandleExpressCallback(string orderId, string contactId, string marketId, string cartName, PaymentCallback paymentCallback)
        {
            await _vippsOrderCreator.ProcessPaymentCallback(paymentCallback, orderId, contactId, marketId, cartName)
                                    .ConfigureAwait(false);

            return HttpStatusCode.OK;
        }

        public virtual ShippingDetailsResponse GetShippingDetails(string orderId, string contactId, string marketId, string cartName, ShippingRequest shippingRequest)
        {
            var cart = _vippsService.GetCartByContactId(contactId, marketId, cartName);

            var shippingMethods = ShippingManager.GetShippingMethodsByMarket(cart.MarketId.Value, false).ShippingMethod.ToList().OrderBy(x=>x.Ordering);

            var shippingDetails = new List<ShippingDetails>();

            var counter = 1;

            foreach (var shippingMethod in shippingMethods)
            {
                lock (cart)
                {
                    var shipment = cart.GetFirstShipment();
                    shipment.ShippingAddress = AddressHelper.ShippingRequestToOrderAddress(shippingRequest, cart);
                    shipment.ShippingMethodId = shippingMethod.ShippingMethodId;
                    cart.ApplyDiscounts();

                    shippingDetails.Add(new ShippingDetails
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

        private static void UpdateShipment(ICart cart, IOrderAddress orderAddress, ShippingDetails shippingDetails)
        {
            var shipment = cart.GetFirstShipment();
            shipment.ShippingMethodId = new Guid(shippingDetails.ShippingMethodId);
            shipment.ShippingAddress = orderAddress;
        }

        private void UpdatePayment(ICart cart, IPayment payment, IOrderAddress orderAddress, TransactionInfo transactionInfo, string orderId)
        {
            cart.GetFirstForm().Payments.Clear();
            var total = cart.GetTotal(_orderGroupCalculator);
            var newPayment = PaymentHelper.CreateVippsPayment(cart, total, payment.PaymentMethodId);
            newPayment.Status = PaymentStatus.Processed.ToString();
            cart.AddPayment(newPayment, _orderGroupFactory);
            newPayment.BillingAddress = orderAddress;
            newPayment.TransactionID = orderId;
        }
    }
}
