using System;
using System.Web.Mvc;
using EPiServer.Reference.Commerce.Site.Features.VippsTest.Models;
using Mediachase.Commerce;
using Vipps;
using Vipps.Extensions;
using Vipps.Helpers;
using Vipps.Models;
using Vipps.Models.Partials;
using Vipps.Models.RequestModels;
using Vipps.Services;

namespace EPiServer.Reference.Commerce.Site.Features.VippsTest.Controllers
{
    public class VippsTestController : Controller
    {
        private readonly VippsServiceApiFactory _vippsServiceApiFactory;
        private readonly IVippsService _vippsService;
        private readonly IVippsConfigurationLoader _configurationLoader;
        private readonly ICurrentMarket _currentMarket;

        public VippsTestController(VippsServiceApiFactory vippsServiceApiFactory,
            IVippsService vippsService, IVippsConfigurationLoader configurationLoader,
            ICurrentMarket currentMarket)
        {
            _vippsServiceApiFactory = vippsServiceApiFactory;
            _vippsService = vippsService;
            _configurationLoader = configurationLoader;
            _currentMarket = currentMarket;
        }

        // GET: VippsTest
        public ActionResult Index()
        {
            var viewModel = new VippsTestViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Initiate()
        {
            var configuration = _configurationLoader.GetConfiguration(_currentMarket.GetCurrentMarket().MarketId);
            var vippsApi = _vippsServiceApiFactory.Create(configuration);
            var viewModel = new VippsTestViewModel();

            try
            {
                var orderId = OrderNumberHelper.GenerateOrderNumber();
                var initiatePaymentRequest = new InitiatePaymentRequest
                {
                    CustomerInfo = new CustomerInfo(),
                    MerchantInfo = new MerchantInfo
                    {
                        CallbackPrefix = EnsureCorrectUrl(configuration.SiteBaseUrl, "vippscallback/"),
                        ConsentRemovalPrefix = EnsureCorrectUrl(configuration.SiteBaseUrl, "vippscallback/"),
                        FallBack = EnsureCorrectUrl(configuration.SiteBaseUrl, $"vippstest/fallback?orderId={orderId}"),
                        ShippingDetailsPrefix = EnsureCorrectUrl(configuration.SiteBaseUrl, "vippscallback/"),
                        IsApp = false,
                        MerchantSerialNumber = Convert.ToInt32(configuration.MerchantSerialNumber),
                        PaymentType = VippsConstants.RegularCheckout

                    },
                    Transaction = new Transaction
                    {
                        Amount = 20000,
                        OrderId = orderId,
                        TimeStamp = DateTime.Now,
                        TransactionText = "test"
                    }
                };

                var result = vippsApi.Initiate(initiatePaymentRequest).Result;

                return Redirect(result.Url);
            }

            catch (Exception ex)
            {
                viewModel.Message = $"Initiate payment failed. Exception: {ex.Message} {ex.InnerException} {ex.StackTrace}";
                return View("Index", viewModel);
            }
        }

        public ActionResult Fallback(string orderId, string contactId, string marketId, string cartName)
        {
            var viewModel = new VippsTestViewModel
            {
                VippsTestForm = new VippsTestForm
                {
                    OrderId = orderId
                }
            };

            try
            {
                var status = _vippsService.GetOrderStatusAsync(orderId, marketId).Result;
                if (status.TransactionInfo.Status == VippsStatusResponseStatus.RESERVE.ToString())
                {
                    viewModel.Step = VippsUpdatePaymentResponseStatus.Initiate.ToString();
                    viewModel.Message = $"Payment successfully initiated. Vipps order id: {orderId}";
                }

                else
                {
                    viewModel.Message= $"Payment not initiated. Vipps order id: {orderId}. Payment status: {status.TransactionInfo.Status}";
                }
            }

            catch (Exception ex)
            {
                viewModel.Message =
                    $"Something went wrong on initializing payment or calling payment status endpoint. Exception: {ex.Message} {ex.InnerException} {ex.StackTrace}";
            }

            return View("Index", viewModel);
        }

        [HttpPost]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Capture(VippsTestViewModel model)
        {
            var configuration = _configurationLoader.GetConfiguration(_currentMarket.GetCurrentMarket().MarketId);
            var vippsApi = _vippsServiceApiFactory.Create(configuration);

            var viewModel = new VippsTestViewModel
            {
                VippsTestForm = new VippsTestForm
                {
                    OrderId = model.VippsTestForm.OrderId
                }
            };

            try
            {
                var result = vippsApi.Capture(model.VippsTestForm.OrderId, new UpdatePaymentRequest
                {
                    MerchantInfo = new MerchantInfo
                    {
                        MerchantSerialNumber = Convert.ToInt32(configuration.MerchantSerialNumber)
                    },
                    Transaction = new Transaction
                    {
                        Amount = 20000,
                        TransactionText = "Test vipps capture"
                    }

                }).Result;

                if (result.TransactionInfo.Status == VippsUpdatePaymentResponseStatus.Captured.ToString())
                {
                    viewModel.Message = $"Order with vipps orderId {model.VippsTestForm.OrderId} successfully captured";
                    viewModel.Step = VippsUpdatePaymentResponseStatus.Captured.ToString();
                }

                else
                {
                    viewModel.Message = $"Capture failed for order with vipps orderId {model.VippsTestForm.OrderId}. Order status: {result.TransactionInfo.Status}";
                    viewModel.Step = VippsUpdatePaymentResponseStatus.Initiate.ToString();
                }
            }

            catch (Exception ex)
            {
                viewModel.Message = $"Capture failed for order with vipps orderId {model.VippsTestForm.OrderId}. Exception: {ex.Message} {ex.InnerException} {ex.StackTrace}";
                viewModel.Step = VippsUpdatePaymentResponseStatus.Initiate.ToString();
            }

            return View("Index", viewModel);
        }

        [HttpPost]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Refund(VippsTestViewModel model)
        {
            var configuration = _configurationLoader.GetConfiguration(_currentMarket.GetCurrentMarket().MarketId);
            var vippsApi = _vippsServiceApiFactory.Create(configuration);

            var viewModel = new VippsTestViewModel
            {
                VippsTestForm = new VippsTestForm
                {
                    OrderId = model.VippsTestForm.OrderId
                }
            };

            try
            {
                var result = vippsApi.Refund(model.VippsTestForm.OrderId, new UpdatePaymentRequest
                {
                    MerchantInfo = new MerchantInfo
                    {
                        MerchantSerialNumber = Convert.ToInt32(configuration.MerchantSerialNumber)
                    },
                    Transaction = new Transaction
                    {
                        Amount = 20000,
                        TransactionText = "Vipps test refund"
                    }

                }).Result;

                if (result.TransactionInfo != null && result.TransactionInfo.Status == VippsUpdatePaymentResponseStatus.Refund.ToString() || result.TransactionSummary.RefundedAmount == result.TransactionSummary.CapturedAmount)
                {
                    viewModel.Message = $"Order with vipps orderId {model.VippsTestForm.OrderId} successfully refunded";
                    viewModel.Step = VippsUpdatePaymentResponseStatus.Refund.ToString();
                }

                else
                {
                    viewModel.Message = $"Refund failed for order with vipps orderId {model.VippsTestForm.OrderId}. Order status: {result.TransactionInfo.Status}";
                    viewModel.Step = VippsUpdatePaymentResponseStatus.Captured.ToString();
                }
            }

            catch (Exception ex)
            {
                viewModel.Message = $"Refund failed for order with vipps orderId {model.VippsTestForm.OrderId}. Exception: {ex.Message} {ex.InnerException} {ex.StackTrace}";
                viewModel.Step = VippsUpdatePaymentResponseStatus.Captured.ToString();
            }

            return View("Index", viewModel);
        }

        [HttpPost]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Cancel(VippsTestViewModel model)
        {
            var configuration = _configurationLoader.GetConfiguration(_currentMarket.GetCurrentMarket().MarketId);
            var vippsApi = _vippsServiceApiFactory.Create(configuration);

            var viewModel = new VippsTestViewModel
            {
                VippsTestForm = new VippsTestForm
                {
                    OrderId = model.VippsTestForm.OrderId
                }
            };

            try
            {
                var result = vippsApi.Cancel(model.VippsTestForm.OrderId, new UpdatePaymentRequest
                {
                    MerchantInfo = new MerchantInfo
                    {
                        MerchantSerialNumber = Convert.ToInt32(configuration.MerchantSerialNumber)
                    },
                    Transaction = new Transaction
                    {
                        TransactionText = "Vipps test cancel",
                    }

                }).Result;

                if (result.TransactionInfo.Status == VippsUpdatePaymentResponseStatus.Cancelled.ToString())
                {
                    viewModel.Message = $"Order with vipps orderId {model.VippsTestForm.OrderId} successfully cancelled";
                    viewModel.Step = VippsUpdatePaymentResponseStatus.Cancelled.ToString();
                }

                else
                {
                    viewModel.Message = $"Cancel failed for order with vipps orderId {model.VippsTestForm.OrderId}. Order status: {result.TransactionInfo.Status}";
                    viewModel.Step = VippsUpdatePaymentResponseStatus.Captured.ToString();
                }
            }

            catch (Exception ex)
            {
                viewModel.Message = $"Cancel failed for order with vipps orderId {model.VippsTestForm.OrderId}. Exception: {ex.Message} {ex.InnerException} {ex.StackTrace}";
                viewModel.Step = VippsUpdatePaymentResponseStatus.Captured.ToString();
            }

            return View("Index", viewModel);
        }

        private string EnsureCorrectUrl(string siteBaseUrl, string path)
        {
            if (siteBaseUrl.EndsWith("/"))
            {
                return $"{siteBaseUrl}{path}";
            }

            return $"{siteBaseUrl}/{path}";
        }
    }
}