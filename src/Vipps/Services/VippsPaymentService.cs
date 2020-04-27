using System;
using System.Linq;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Managers;
using Refit;
using Vipps.Extensions;
using Vipps.Helpers;
using Vipps.Models;
using Vipps.Polling;

namespace Vipps.Services
{
    [ServiceConfiguration(typeof(IVippsPaymentService))]
    public class VippsPaymentService : VippsServiceBase, IVippsPaymentService
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(VippsPaymentService));
        private readonly IOrderRepository _orderRepository;
        private readonly VippsServiceApiFactory _vippsServiceApiFactory;
        private readonly IVippsRequestFactory _requestFactory;
        private readonly IVippsOrderProcessor _vippsOrderCreator;
        private readonly IVippsService _vippsService;
        private readonly IVippsPollingService _vippsPollingService;
        private readonly IVippsConfigurationLoader _configurationLoader;

        public VippsPaymentService(
            IOrderRepository orderRepository,
            VippsServiceApiFactory vippsServiceApiFactory, 
            IVippsRequestFactory requestFactory,
            IVippsOrderProcessor vippsOrderCreator,
            IVippsService vippsService,
            IVippsPollingService vippsPollingService,
            IVippsConfigurationLoader configurationLoader)
            {
            _orderRepository = orderRepository;
            _vippsServiceApiFactory = vippsServiceApiFactory;
            _requestFactory = requestFactory;
            _vippsOrderCreator = vippsOrderCreator;
            _vippsService = vippsService;
            _vippsPollingService = vippsPollingService;
            _configurationLoader = configurationLoader;
            ;
        }

        #region public

        public virtual async Task<PaymentProcessingResult> InitiateAsync(IOrderGroup orderGroup, IPayment payment)
        {
            var orderId = OrderNumberHelper.GenerateOrderNumber();
            orderGroup.Properties[VippsConstants.VippsOrderIdField] = orderId;
            payment.TransactionID = orderId;
            _orderRepository.Save(orderGroup);

            var configuration = _configurationLoader.GetConfiguration(orderGroup.MarketId);

            var serviceApi = _vippsServiceApiFactory.Create(configuration);

            try
            {
                var initiatePaymentRequest =
                    _requestFactory.CreateInitiatePaymentRequest(payment, orderGroup, configuration, orderId, orderGroup.CustomerId, orderGroup.MarketId.Value);

                var response = await serviceApi.Initiate(initiatePaymentRequest).ConfigureAwait(false);

                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps payment initiated. Vipps reference: {initiatePaymentRequest.Transaction.OrderId}");

                _vippsPollingService.Start(orderId, orderGroup);

                return PaymentProcessingResult.CreateSuccessfulResult("", response.Url);
            }

            catch (ApiException apiException)
            {
                var errorMessage = GetErrorMessage(apiException);
                _logger.Log(Level.Error, $"Vipps initiate failed: {errorMessage}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"Vipps payment initiation failed. Error message: {errorMessage}, {apiException}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(errorMessage);
            }

            catch (Exception ex)
            {
                _logger.Log(Level.Error, $"{ex.Message}, {ex.StackTrace}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"Vipps payment initiation failed. Error message: {ex.Message}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(ex.Message);
            }
        }

        public virtual async Task<PaymentProcessingResult> CaptureAsync(IOrderGroup orderGroup, IPayment payment)
        {
            var configuration = _configurationLoader.GetConfiguration(orderGroup.MarketId);

            try
            {
                var orderId = payment.TransactionID;
                var serviceApi = _vippsServiceApiFactory.Create(configuration);

                var capturePaymentRequest =
                    _requestFactory.CreateUpdatePaymentRequest(payment, configuration);
                var response = await serviceApi.Capture(orderId, capturePaymentRequest).ConfigureAwait(false);

                if (response.TransactionInfo.Status == VippsUpdatePaymentResponseStatus.Captured.ToString())
                {
                    OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"{payment.Amount} kr captured on vipps order {orderId}");
                    return PaymentProcessingResult.CreateSuccessfulResult($"{payment.Amount} kr captured on vipps order {orderId}");
                }

                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"Vipps capture payment failed. Order status: {response.TransactionInfo.Status}");
                return PaymentProcessingResult.CreateUnsuccessfulResult($"Vipps capture payment failed. Order status: {response.TransactionInfo.Status}");
            }

            catch (ApiException apiException)
            {
                var errorMessage = GetErrorMessage(apiException);
                _logger.Log(Level.Error, $"Vipps Capture failed: {errorMessage}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"Vipps capture payment failed. Error message: {errorMessage}, {apiException}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(errorMessage);
            }

            catch (Exception ex)
            {
                _logger.Log(Level.Error, $"{ex.Message}, {ex.StackTrace}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"Vipps capture payment failed. Error message: {ex.Message}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(ex.Message);
            }
        }

        public virtual async Task<PaymentProcessingResult> RefundAsync(IOrderGroup orderGroup, IPayment payment)
        {
            var configuration = _configurationLoader.GetConfiguration(orderGroup.MarketId);

            try
            {
                var orderId = payment.TransactionID;
                var serviceApi = _vippsServiceApiFactory.Create(configuration);

                var refundPaymentRequest =
                    _requestFactory.CreateUpdatePaymentRequest(payment, configuration);

                var response = await serviceApi.Refund(orderId, refundPaymentRequest).ConfigureAwait(false);

                if (response.TransactionSummary.RefundedAmount == payment.Amount.FormatAmountToVipps())
                {
                    OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"{payment.Amount} kr refunded on vipps order {orderId}");
                    return PaymentProcessingResult.CreateSuccessfulResult($"{payment.Amount} kr captured on refunded order {orderId}");
                }

                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"Vipps refund payment failed. Order status: {response.TransactionInfo.Status}");
                return PaymentProcessingResult.CreateUnsuccessfulResult($"Vipps refund payment failed. Order status: {response.TransactionInfo.Status}");
            }

            catch (ApiException apiException)
            {
                var errorMessage = GetErrorMessage(apiException);
                _logger.Log(Level.Error, $"Vipps Refund failed: {errorMessage}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"Vipps refund payment failed. Error message: {errorMessage}, {apiException}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(errorMessage);
            }

            catch (Exception ex)
            {
                _logger.Log(Level.Error, $"{ex.Message}, {ex.StackTrace}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"Vipps refund payment failed. Error message: {ex.Message}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(ex.Message);
            }
        }

        public virtual async Task<PaymentProcessingResult> CancelAsync(IOrderGroup orderGroup, IPayment payment)
        {
            var configuration = _configurationLoader.GetConfiguration(orderGroup.MarketId);

            try
            {
                var orderId = payment.TransactionID;
                var serviceApi = _vippsServiceApiFactory.Create(configuration);

                var cancelPaymentRequest =
                    _requestFactory.CreateUpdatePaymentRequest(payment, configuration);

                var response = await serviceApi.Cancel(orderId, cancelPaymentRequest).ConfigureAwait(false);

                if (response.TransactionInfo.Status == VippsUpdatePaymentResponseStatus.Cancelled.ToString())
                {
                    OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"Payment cancelled for vipps order {orderId}");
                    return PaymentProcessingResult.CreateSuccessfulResult($"{payment.Amount} Payment cancelled vipps order {orderId}");
                }

                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"Vipps cancel payment failed. Order status: {response.TransactionInfo.Status}");
                return PaymentProcessingResult.CreateUnsuccessfulResult($"Vipps cancel payment failed. Order status: {response.TransactionInfo.Status}");
            }

            catch (ApiException apiException)
            {
                var errorMessage = GetErrorMessage(apiException); 
                _logger.Log(Level.Error, $"Vipps cancel failed: {errorMessage}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"Vipps cancel payment failed. Error message: {errorMessage}, {apiException}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(errorMessage);
            }

            catch (Exception ex)
            {
                _logger.Log(Level.Error, $"{ex.Message}, {ex.StackTrace}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType, $"Vipps cancel payment failed. Error message: {ex.Message}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(ex.Message);
            }
        }

        public virtual async Task<ProcessAuthorizationResponse> ProcessAuthorizationAsync(Guid contactId, string marketId, string cartName, string orderId)
        {
            var purchaseOrder = _vippsService.GetPurchaseOrderByOrderId(orderId);
            if (purchaseOrder != null)
            {
                return new ProcessAuthorizationResponse
                {
                    PurchaseOrder = purchaseOrder,
                    Processed = true
                };
            }

            var cart = _vippsService.GetCartByContactId(contactId, marketId, cartName);
            var paymentType = GetVippsPaymentType(cart);

            var orderDetails = await _vippsService.GetOrderDetailsAsync(orderId, marketId);
            var result = await _vippsOrderCreator.ProcessOrderDetails(orderDetails, orderId, contactId, marketId, cartName);
            if (result.PurchaseOrder != null)
            {
                return new ProcessAuthorizationResponse(result)
                {
                    Processed = true
                };
            }

            return new ProcessAuthorizationResponse(result)
            {
                PaymentType = paymentType,
                Processed = false
            };
        }

        #endregion

        #region private

        private static IPayment GetPayment(ICart cart, string orderId)
        {
            if (cart.Forms == null || cart.Forms.Count == 0 || cart.GetFirstForm().Payments == null ||
                cart.GetFirstForm().Payments.Count == 0)
            {
                return null;
            }

            var payments = cart.GetFirstForm().Payments;

            return payments.Any()
                ? payments.FirstOrDefault(x => x.TransactionType.Equals(TransactionType.Authorization.ToString()) && x.IsVippsPayment() && x.TransactionID == orderId)
                : null;
        }

        private VippsPaymentType GetVippsPaymentType(ICart cart)
        {
            if (cart == null)
            {
                return VippsPaymentType.UNKNOWN;
            }

            if (Enum.TryParse<VippsPaymentType>(cart?.Properties[VippsConstants.VippsPaymentTypeField]?.ToString(),
                out var paymentType))
            {
                return paymentType;
            }

            return VippsPaymentType.CHECKOUT;
        }

        #endregion
    }
}
