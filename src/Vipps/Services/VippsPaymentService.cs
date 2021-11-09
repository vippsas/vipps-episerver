using System;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
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
        private readonly IVippsPollingService _vippsPollingService;
        private readonly IVippsConfigurationLoader _configurationLoader;

        public VippsPaymentService(
            IOrderRepository orderRepository,
            VippsServiceApiFactory vippsServiceApiFactory,
            IVippsRequestFactory requestFactory,
            IVippsOrderProcessor vippsOrderCreator,
            IVippsPollingService vippsPollingService,
            IVippsConfigurationLoader configurationLoader)
        {
            _orderRepository = orderRepository;
            _vippsServiceApiFactory = vippsServiceApiFactory;
            _requestFactory = requestFactory;
            _vippsOrderCreator = vippsOrderCreator;
            _vippsPollingService = vippsPollingService;
            _configurationLoader = configurationLoader;
        }

        #region public

        public virtual PaymentProcessingResult Initiate(
            IOrderGroup orderGroup,
            IPayment payment)
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
                    _requestFactory.CreateInitiatePaymentRequest(payment, orderGroup, configuration, orderId,
                        orderGroup.CustomerId, orderGroup.MarketId.Value);

                var response = AsyncHelper.RunSync(() => serviceApi.Initiate(initiatePaymentRequest));

                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps payment initiated. Vipps reference: {initiatePaymentRequest.Transaction.OrderId}");

                _vippsPollingService.Start(orderId, orderGroup);

                return PaymentProcessingResult.CreateSuccessfulResult("", response.Url);
            }

            catch (ApiException apiException)
            {
                var errorMessage = GetErrorMessage(apiException);
                _logger.Log(Level.Error, $"Vipps initiate failed: {errorMessage}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps payment initiation failed. Error message: {errorMessage}, {apiException}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(errorMessage);
            }

            catch (Exception ex)
            {
                _logger.Log(Level.Error, $"{ex.Message}, {ex.StackTrace}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps payment initiation failed. Error message: {ex.Message}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(ex.Message);
            }
        }

        public virtual PaymentProcessingResult Capture(
            IOrderGroup orderGroup,
            IPayment payment)
        {
            var configuration = _configurationLoader.GetConfiguration(orderGroup.MarketId);

            try
            {
                var orderId = payment.TransactionID;
                var idempotencyKey = GetIdempotencyKey(orderGroup, payment, orderId);
                var serviceApi = _vippsServiceApiFactory.Create(configuration);

                var capturePaymentRequest =
                    _requestFactory.CreateUpdatePaymentRequest(payment, configuration);

                var response = AsyncHelper.RunSync(() => serviceApi.Capture(orderId, capturePaymentRequest, idempotencyKey));

                if (response.TransactionInfo.Status == VippsUpdatePaymentResponseStatus.Captured.ToString())
                {
                    OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                        $"{payment.Amount} kr captured on vipps order {orderId}");
                    return PaymentProcessingResult.CreateSuccessfulResult(
                        $"{payment.Amount} kr captured on vipps order {orderId}");
                }

                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps capture payment failed. Order status: {response.TransactionInfo.Status}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(
                    $"Vipps capture payment failed. Order status: {response.TransactionInfo.Status}");
            }

            catch (ApiException apiException)
            {
                var errorMessage = GetErrorMessage(apiException);
                _logger.Log(Level.Error, $"Vipps Capture failed: {errorMessage}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps capture payment failed. Error message: {errorMessage}, {apiException}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(errorMessage);
            }

            catch (Exception ex)
            {
                _logger.Log(Level.Error, $"{ex.Message}, {ex.StackTrace}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps capture payment failed. Error message: {ex.Message}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(ex.Message);
            }
        }

        public virtual PaymentProcessingResult Refund(
            IOrderGroup orderGroup,
            IPayment payment)
        {
            var configuration = _configurationLoader.GetConfiguration(orderGroup.MarketId);

            try
            {
                var orderId = payment.TransactionID;
                var serviceApi = _vippsServiceApiFactory.Create(configuration);

                var refundPaymentRequest =
                    _requestFactory.CreateUpdatePaymentRequest(payment, configuration);

                var response = AsyncHelper.RunSync(() => serviceApi.Refund(orderId, refundPaymentRequest));
                
                var creditTotal = orderGroup.GetFirstForm().Payments.Where(x => x.TransactionType == "Credit")
                    .Sum(x => x.Amount).FormatAmountToVipps();
                
                if (response.TransactionSummary.RefundedAmount == creditTotal)
                {
                    OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                        $"{payment.Amount} kr refunded on vipps order {orderId}");
                    return PaymentProcessingResult.CreateSuccessfulResult(
                        $"{payment.Amount} kr captured on refunded order {orderId}");
                }

                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps refund payment failed. Order status: {response.TransactionInfo.Status}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(
                    $"Vipps refund payment failed. Order status: {response.TransactionInfo.Status}");
            }

            catch (ApiException apiException)
            {
                var errorMessage = GetErrorMessage(apiException);
                _logger.Log(Level.Error, $"Vipps Refund failed: {errorMessage}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps refund payment failed. Error message: {errorMessage}, {apiException}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(errorMessage);
            }

            catch (Exception ex)
            {
                _logger.Log(Level.Error, $"{ex.Message}, {ex.StackTrace}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps refund payment failed. Error message: {ex.Message}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(ex.Message);
            }
        }

        public virtual PaymentProcessingResult Cancel(
            IOrderGroup orderGroup,
            IPayment payment)
        {
            var configuration = _configurationLoader.GetConfiguration(orderGroup.MarketId);

            try
            {
                var orderId = payment.TransactionID;
                var serviceApi = _vippsServiceApiFactory.Create(configuration);

                var cancelPaymentRequest =
                    _requestFactory.CreateUpdatePaymentRequest(payment, configuration);

                var response = AsyncHelper.RunSync(() => serviceApi.Cancel(payment.TransactionID, cancelPaymentRequest));
                if (response.TransactionInfo.Status == VippsUpdatePaymentResponseStatus.Cancelled.ToString())
                {
                    OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                        $"Payment cancelled for vipps order {orderId}");
                    return PaymentProcessingResult.CreateSuccessfulResult(
                        $"{payment.Amount} Payment cancelled vipps order {orderId}");
                }

                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps cancel payment failed. Order status: {response.TransactionInfo.Status}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(
                    $"Vipps cancel payment failed. Order status: {response.TransactionInfo.Status}");
            }

            catch (ApiException apiException)
            {
                var errorMessage = GetErrorMessage(apiException);
                _logger.Log(Level.Error, $"Vipps cancel failed: {errorMessage}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps cancel payment failed. Error message: {errorMessage}, {apiException}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(errorMessage);
            }

            catch (Exception ex)
            {
                payment.Status = PaymentStatus.Failed.ToString();
                _logger.Log(Level.Error, $"{ex.Message}, {ex.StackTrace}");
                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps cancel payment failed. Error message: {ex.Message}");
                return PaymentProcessingResult.CreateUnsuccessfulResult(ex.Message);
            }
        }

        public virtual ProcessAuthorizationResponse ProcessAuthorization(
            Guid contactId,
            string marketId,
            string cartName,
            string orderId)
        {
            var result = _vippsOrderCreator.FetchAndProcessOrderDetails(orderId, contactId, marketId, cartName);
            if (result.PurchaseOrder != null)
            {
                return new ProcessAuthorizationResponse(result)
                {
                    Processed = true
                };
            }

            return new ProcessAuthorizationResponse(result)
            {
                Processed = false
            };
        }

        #endregion

        #region private

        private static IPayment GetPayment(
            ICart cart, 
            string orderId)
        {
            if (cart.Forms == null || cart.Forms.Count == 0 || cart.GetFirstForm().Payments == null ||
                cart.GetFirstForm().Payments.Count == 0)
            {
                return null;
            }

            var payments = cart.GetFirstForm().Payments;

            return payments.Any()
                ? payments.FirstOrDefault(x =>
                    x.TransactionType.Equals(TransactionType.Authorization.ToString()) && x.IsVippsPayment() &&
                    x.TransactionID == orderId)
                : null;
        }

        private string GetIdempotencyKey(IOrderGroup orderGroup, IPayment payment, string orderId)
        {
            return $"{orderId}-{string.Format("{0:0.00}", orderGroup.GetFirstForm().CapturedPaymentTotal)}-{string.Format("{0:0.00}", payment.Amount)}";
        }

        #endregion
    }
}