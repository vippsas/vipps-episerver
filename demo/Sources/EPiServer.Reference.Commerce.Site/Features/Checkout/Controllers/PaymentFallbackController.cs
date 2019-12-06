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
using EPiServer.Security;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Security;
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
        private readonly ICurrentMarket _currentMarket;
        private readonly IVippsService _vippsService;

        public PaymentFallbackController(ICartService cartService,
            IOrderRepository orderRepository,
            IVippsPaymentService vippsPaymentService,
            ReferenceConverter referenceConverter,
            IContentLoader contentLoader,
            CustomerContextFacade customerContext,
            ICurrentMarket currentMarket,
            IVippsService vippsService)
        {
            _cartService = cartService;
            _orderRepository = orderRepository;
            _vippsPaymentService = vippsPaymentService;
            _referenceConverter = referenceConverter;
            _contentLoader = contentLoader;
            _customerContext = customerContext;
            _currentMarket = currentMarket;
            _vippsService = vippsService;
        }

        public async Task<RedirectResult> Index(string orderId)
        {
            var currentContactId = PrincipalInfo.CurrentPrincipal.GetContactId();
            var currentMarketId = _currentMarket.GetCurrentMarket().MarketId.Value;
            var result = await _vippsPaymentService.ProcessAuthorizationAsync(currentContactId, currentMarketId, orderId);

            //If ProcessAuthorization fails user needs to be redirected back to checkout or product page
            if (!result.Processed)
            {
                if (result.PaymentType == VippsPaymentType.CHECKOUT)
                {
                    //Redirect to checkout (preferably with error message)
                    return new RedirectResult("/en/checkout");
                }

                //Redirect back to product if express checkout (preferably with error message)
                if (result.PaymentType == VippsPaymentType.PRODUCTEXPRESS)
                {
                    var cart = _vippsService.GetCartByContactId(currentContactId, currentMarketId, orderId);
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
    }
}