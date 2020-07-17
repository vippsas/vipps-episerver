using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.Security;
using Mediachase.Commerce.Security;
using Vipps.Helpers;
using Vipps.Models;

namespace Vipps.Controllers
{
    [RoutePrefix("vippsexpress")]
    public class VippsExpressController : ApiController
    {
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(VippsExpressController));

        public VippsExpressController(IOrderGroupCalculator orderGroupCalculator,
            IOrderRepository orderRepository)
        {
            _orderGroupCalculator = orderGroupCalculator;
            _orderRepository = orderRepository;
        }

        [HttpGet]
        [Route("cartexpress")]
        [ResponseType(typeof(ExpressCheckoutResponse))]
        public IHttpActionResult CartExpress()
        {
            var cart = _orderRepository.LoadCart<ICart>(PrincipalInfo.CurrentPrincipal.GetContactId(),
                "Default");

            if (cart == null)
            {
                return GetUnsuccessfulResult("No cart found");
            }

            cart.Properties[VippsConstants.VippsPaymentTypeField] = VippsPaymentType.CARTEXPRESS;

            return Finalize(cart);
        }

        [HttpGet]
        [Route("wishlistexpress")]
        [ResponseType(typeof(ExpressCheckoutResponse))]
        public IHttpActionResult WishListExpress()
        {
            var wishListCart = _orderRepository.LoadCart<ICart>(PrincipalInfo.CurrentPrincipal.GetContactId(),
                "WishList");

            if (wishListCart == null)
            {
                return GetUnsuccessfulResult("No wish list found");
            }

            var cart =  _orderRepository.LoadOrCreateCart<ICart>(PrincipalInfo.CurrentPrincipal.GetContactId(),
                VippsConstants.VippsSingleProductCart);
            VippsExpressCartHelper.RemoveAllLineItems(cart);

            foreach (var lineItem in wishListCart.GetAllLineItems())
            {
                VippsExpressCartHelper.AddToCart(cart, lineItem.Code, lineItem.Quantity);
            }

            cart.Properties[VippsConstants.VippsPaymentTypeField] = VippsPaymentType.WISHLISTEXPRESS;

            return Finalize(cart);
        }

        [HttpPost]
        [Route("productexpress")]
        [ResponseType(typeof(ExpressCheckoutResponse))]
        public IHttpActionResult ProductExpress(string code, decimal quantity)
        {
            var cart =  _orderRepository.LoadOrCreateCart<ICart>(PrincipalInfo.CurrentPrincipal.GetContactId(),
                VippsConstants.VippsSingleProductCart);

            VippsExpressCartHelper.RemoveAllLineItems(cart);
            VippsExpressCartHelper.AddToCart(cart, code, quantity);
            var validationIssues = VippsExpressCartHelper.ValidateCart(cart);

            if (validationIssues.Any())
            {
                return GetUnsuccessfulResult(validationIssues.FirstOrDefault().ToString());
            }

            cart.Properties[VippsConstants.VippsPaymentTypeField] = VippsPaymentType.PRODUCTEXPRESS;

            return Finalize(cart);
        }

        private IHttpActionResult Finalize(ICart cart)
        {
            cart.ApplyDiscounts();
            var total = _orderGroupCalculator.GetSubTotal(cart);
            var vippsPaymentMethodDto = PaymentHelper.GetVippsPaymentMethodDto().PaymentMethod.FirstOrDefault();

            if (vippsPaymentMethodDto == null)
            {
                _logger.Warning("No payment method is setup for vipps");
                return GetUnsuccessfulResult("No payment method is setup for vipps");
            }

            cart.GetFirstForm().Payments.Clear();
            var payment = PaymentHelper.CreateVippsPayment(cart, total, vippsPaymentMethodDto.PaymentMethodId);
            cart.AddPayment(payment);

            _orderRepository.Save(cart);

            try
            {
                var paymentProcessingResults = cart.ProcessPayments().ToArray();
                var successfulResult = paymentProcessingResults.FirstOrDefault(x => x.IsSuccessful && !string.IsNullOrEmpty(x.RedirectUrl));

                if (successfulResult != null)
                {
                    return GetSuccessfulResult(paymentProcessingResults.FirstOrDefault(x=>x.IsSuccessful) ?? paymentProcessingResults.FirstOrDefault());
                }

                return GetUnsuccessfulResult(paymentProcessingResults.FirstOrDefault(x => !string.IsNullOrEmpty(x.Message))?.Message);
            }

            catch(Exception ex)
            {
                _logger.Error($"{ex.Message} {ex.StackTrace}");
                return GetUnsuccessfulResult(ex.Message);
            }
        }

        private IHttpActionResult GetUnsuccessfulResult(string errorMessage)
        {
            return Ok(new ExpressCheckoutResponse
            {
                ErrorMessage = errorMessage,
                Success = false
            });
        }

        private IHttpActionResult GetSuccessfulResult(PaymentProcessingResult paymentProcessingResult)
        {
            return Ok(new ExpressCheckoutResponse
            {
                Success = paymentProcessingResult.IsSuccessful,
                ErrorMessage = paymentProcessingResult.Message,
                RedirectUrl = paymentProcessingResult.RedirectUrl
            });
        }
}
}
