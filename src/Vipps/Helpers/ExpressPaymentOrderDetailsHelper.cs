using System;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Vipps.Models.ResponseModels;

namespace Vipps.Helpers
{
    public static class ExpressPaymentOrderDetailsHelper
    {
        private static Injected<IOrderRepository> _orderRepository;

        public static void EnsureExpressPaymentAndShipping(ICart cart, IPayment payment, DetailsResponse orderDetails)
        {
            if (cart.GetFirstShipment().ShippingMethodId == default(Guid) ||
                cart.GetFirstShipment().ShippingAddress == null ||
                payment?.BillingAddress == null)
            {
                EnsureShipping(cart, orderDetails);
                EnsureBillingAddress(payment, cart, orderDetails);

                _orderRepository.Service.Save(cart);
            }
        }

        private static void EnsureBillingAddress(IPayment payment, ICart cart, DetailsResponse details)
        {
            if (payment.BillingAddress == null)
            {
                payment.BillingAddress =
                    AddressHelper.UserDetailsAndShippingDetailsToOrderAddress(details.UserDetails,
                        details.ShippingDetails, cart);
            }
        }

        private static void EnsureShipping(ICart cart, DetailsResponse details)
        {
            var shipment = cart.GetFirstShipment();
            if (shipment.ShippingMethodId == default(Guid))
            {
                if (details?.ShippingDetails?.ShippingMethodId != null)
                {
                    shipment.ShippingMethodId = new Guid(details.ShippingDetails.ShippingMethodId);
                }
            }

            if (shipment.ShippingAddress == null)
            {
                shipment.ShippingAddress =
                    AddressHelper.UserDetailsAndShippingDetailsToOrderAddress(details?.UserDetails,
                        details?.ShippingDetails, cart);
            }
        }
    }
}
