using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using EPiServer.Reference.Commerce.Site.Features.Cart.Services;
using EPiServer.Reference.Commerce.Site.Features.Shared.Extensions;
using EPiServer.Reference.Commerce.Site.Infrastructure.Facades;
using Mediachase.Commerce.Catalog;
using Vipps.Models;
using Vipps.Services;

namespace EPiServer.Reference.Commerce.Site.Features.Checkout.Controllers
{
    public class PaymentFallbackController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IVippsPaymentService _vippsPaymentService;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentLoader _contentLoader;
        private readonly CustomerContextFacade _customerContext;
        private readonly ICartService _cartService;
        private readonly IVippsService _vippsService;

        public PaymentFallbackController(ICartService cartService,
            IOrderRepository orderRepository,
            IVippsPaymentService vippsPaymentService,
            ReferenceConverter referenceConverter,
            IContentLoader contentLoader,
            CustomerContextFacade customerContext,
            IVippsService vippsService)
        {
            _cartService = cartService;
            _orderRepository = orderRepository;
            _vippsPaymentService = vippsPaymentService;
            _referenceConverter = referenceConverter;
            _contentLoader = contentLoader;
            _customerContext = customerContext;
            _vippsService = vippsService;
        }

        public async Task<RedirectResult> Index(string orderId, string contactId, string marketId)
        {
            var result = await _vippsPaymentService.ProcessAuthorizationAsync(Guid.Parse(contactId), contactId, orderId);

            //If ProcessAuthorization fails user needs to be redirected back to checkout or product page
            if (!result.Processed)
            {
                //Example method for dealing with different error types and what error message to show
                var errorMessage = GetErrorMessage(result);

                if (result.PaymentType == VippsPaymentType.CHECKOUT)
                {
                    //Redirect to checkout (preferably with error message)
                    return new RedirectResult("/en/checkout");
                }

                //Redirect back to product if express checkout (preferably with error message)
                if (result.PaymentType == VippsPaymentType.PRODUCTEXPRESS)
                {
                    var cart = _vippsService.GetCartByContactId(contactId, marketId, orderId);
                    var item = cart.GetFirstForm().GetAllLineItems().FirstOrDefault();
                    var itemContentLink = _referenceConverter.GetContentLink(item?.Code);
                    var entryContent = _contentLoader.Get<EntryContentBase>(itemContentLink);
                    return new RedirectResult(entryContent.GetUrl());
                }

                //Redirect to cart page if your website has one
                if (result.PaymentType == VippsPaymentType.CARTEXPRESS)
                {
                    return new RedirectResult("/");
                }

                if (result.PaymentType == VippsPaymentType.WISHLISTEXPRESS)
                {
                    return new RedirectResult("/en/my-pages/wish-list/");
                }

                if (result.PaymentType == VippsPaymentType.UNKNOWN)
                {
                    return new RedirectResult("/");
                }
            }


            //If wishlist payment, delete wishlist as well
            if (result.PaymentType == VippsPaymentType.WISHLISTEXPRESS)
            {
                var wishList = _cartService.LoadCart(_cartService.DefaultWishListName);

                if (wishList != null)
                {
                    _orderRepository.Delete(wishList.OrderLink);
                }
            }

            var queryCollection = new NameValueCollection
            {
                {"contactId", _customerContext.CurrentContactId.ToString()},
                {"orderNumber", result.PurchaseOrder.OrderLink.OrderGroupId.ToString(CultureInfo.InvariantCulture)}
            };
            return new RedirectResult(new UrlBuilder("/en/checkout/order-confirmation/") { QueryCollection = queryCollection }.ToString());
        }

        private string GetErrorMessage(ProcessAuthorizationResponse result)
        {
            string errorMessage = string.Empty;

            if (result.ProcessAuthorizationResponseError?.ProcessAuthorizationErrorType == ProcessAuthorizationErrorType.EXCEPTION
                || result.ProcessAuthorizationResponseError?.ProcessAuthorizationErrorType == ProcessAuthorizationErrorType.ORDERVALIDATIONERROR)
            {
                errorMessage = result.ProcessAuthorizationResponseError.ErrorMessage;
            }

            else if (result.ProcessAuthorizationResponseError?.ProcessAuthorizationErrorType == ProcessAuthorizationErrorType.INITIATEFAILED)
            {
                //errorMessage = _myLocalizationService.GetString("vipps/initiatefailed");
            }

            else if (result.ProcessAuthorizationResponseError?.ProcessAuthorizationErrorType == ProcessAuthorizationErrorType.NOCARTFOUND)
            {
                //errorMessage = _myLocalizationService.GetString("vipps/nocartfound");
            }

            else if (result.ProcessAuthorizationResponseError?.ProcessAuthorizationErrorType == ProcessAuthorizationErrorType.NOVIPPSPAYMENTINCART)
            {
                //errorMessage = _myLocalizationService.GetString("vipps/novippspaymentincart");
            }

            return errorMessage;
        }
    }
}