using System;
using System.Linq;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Managers;
using Newtonsoft.Json;
using Refit;
using Vipps.Extensions;
using Vipps.Helpers;
using Vipps.Models;
using Vipps.Models.ResponseModels;

namespace Vipps.Services
{
    [ServiceConfiguration(typeof(IVippsPaymentService))]
    public class VippsPaymentService : IVippsPaymentService
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(VippsPaymentService));
        private readonly IOrderRepository _orderRepository;
        private readonly VippsServiceApiFactory _vippsServiceApiFactory;
        private readonly IVippsRequestFactory _requestFactory;
        private readonly IVippsOrderCreator _vippsOrderCreator;
        private readonly IVippsService _vippsService;

        public VippsPaymentService(
            IOrderRepository orderRepository,
            VippsServiceApiFactory vippsServiceApiFactory,
            IVippsRequestFactory requestFactory,
            IVippsOrderCreator vippsOrderCreator,
            IVippsService vippsService)
        {
            _orderRepository = orderRepository;
            _vippsServiceApiFactory = vippsServiceApiFactory;
            _requestFactory = requestFactory;
            _vippsOrderCreator = vippsOrderCreator;
            _vippsService = vippsService;
        }

        #region public

        public virtual async Task<PaymentProcessingResult> InitiateAsync(IOrderGroup orderGroup, IPayment payment)
        {
            orderGroup.Properties[VippsConstants.VippsOrderIdField] = OrderNumberHelper.GenerateOrderNumber();
            _orderRepository.Save(orderGroup);

            var configuration = PaymentManager.GetPaymentMethod(payment.PaymentMethodId)
                .GetVippsConfiguration();

            var orderId = orderGroup.Properties[VippsConstants.VippsOrderIdField].ToString();

            var serviceApi = _vippsServiceApiFactory.Create(configuration);

            try
            {
                var initiatePaymentRequest =
                    _requestFactory.CreateInitiatePaymentRequest(payment, orderGroup, configuration, orderId, orderGroup.CustomerId, orderGroup.MarketId.Value);

                var response = await serviceApi.Initiate(initiatePaymentRequest).ConfigureAwait(false);

                OrderNoteHelper.AddNoteAndSaveChanges(orderGroup, payment, payment.TransactionType,
                    $"Vipps payment initiated. Vipps reference: {initiatePaymentRequest.Transaction.OrderId}");
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

        public virtual async Task<PaymentProcessingResult> CaptureAsync(IOrderGroup orderGroup, IPayment payment, IShipment shipment)
        {
            var configuration = PaymentManager.GetPaymentMethod(payment.PaymentMethodId)
                .GetVippsConfiguration();

            try
            {
                var orderId = orderGroup.Properties[VippsConstants.VippsOrderIdField].ToString();
                var serviceApi = _vippsServiceApiFactory.Create(configuration);

                var capturePaymentRequest =
                    _requestFactory.CreateUpdatePaymentRequest(payment, orderGroup, shipment, configuration, orderId);
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

        public virtual async Task<PaymentProcessingResult> RefundAsync(IOrderGroup orderGroup, IPayment payment, IShipment shipment)
        {
            var configuration = PaymentManager.GetPaymentMethod(payment.PaymentMethodId)
                .GetVippsConfiguration();

            try
            {
                var orderId = orderGroup.Properties[VippsConstants.VippsOrderIdField].ToString();
                var serviceApi = _vippsServiceApiFactory.Create(configuration);

                var capturePaymentRequest =
                    _requestFactory.CreateUpdatePaymentRequest(payment, orderGroup, shipment, configuration, orderId);

                var response = await serviceApi.Refund(orderId, capturePaymentRequest).ConfigureAwait(false);

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

        public virtual async Task<PaymentProcessingResult> CancelAsync(IOrderGroup orderGroup, IPayment payment, IShipment shipment)
        {
            var configuration = PaymentManager.GetPaymentMethod(payment.PaymentMethodId)
                .GetVippsConfiguration();

            try
            {
                var orderId = orderGroup.Properties[VippsConstants.VippsOrderIdField].ToString();
                var serviceApi = _vippsServiceApiFactory.Create(configuration);

                var capturePaymentRequest =
                    _requestFactory.CreateUpdatePaymentRequest(payment, orderGroup, shipment, configuration, orderId);

                var response = await serviceApi.Cancel(orderId, capturePaymentRequest).ConfigureAwait(false);

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

        public virtual async Task<ProcessAuthorizationResponse> ProcessAuthorizationAsync(Guid contactId, string marketId, string orderId)
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

            var cart = _vippsService.GetCartByContactId(contactId, marketId, orderId);
            if (cart == null)
            {
                _logger.Warning($"No cart found for vipps order id {orderId}");
                return new ProcessAuthorizationResponse
                {
                    PaymentType = VippsPaymentType.UNKNOWN,
                    Processed = false,
                    ProcessAuthorizationResponseError = new ProcessAuthorizationResponseError
                    {
                        ProcessAuthorizationErrorType = ProcessAuthorizationErrorType.NOCARTFOUND
                    }
                };
            }

            var payment = GetPayment(cart);
            var paymentType = GetVippsPaymentType(cart);

            if (payment == null)
            {
                _logger.Warning($"No payment added to cart for vipps order id {orderId}");
                return new ProcessAuthorizationResponse
                {
                    PaymentType = paymentType,
                    Processed = false,
                    ProcessAuthorizationResponseError = new ProcessAuthorizationResponseError
                    {
                        ProcessAuthorizationErrorType = ProcessAuthorizationErrorType.NOVIPPSPAYMENTINCART
                    }
                };
            }

            StatusResponse statusResponse;
            try
            {
                statusResponse = await GetOrderStatusAsync(orderId);
            }

            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                OrderNoteHelper.AddNoteAndSaveChanges(cart, payment, "Query", ex.Message);
                return new ProcessAuthorizationResponse
                {
                    PaymentType = paymentType,
                    Processed = false,
                    ProcessAuthorizationResponseError = new ProcessAuthorizationResponseError
                    {
                        ErrorMessage = ex.Message,
                        ProcessAuthorizationErrorType = ProcessAuthorizationErrorType.EXCEPTION
                    }
                };
            }

            //Set payment type to empty in case cart payment type is changed.
            cart.Properties[VippsConstants.VippsPaymentTypeField] = string.Empty;

            if (statusResponse?.TransactionInfo?.Status == VippsStatusResponseStatus.RESERVE.ToString()
                || statusResponse?.TransactionInfo?.Status == VippsStatusResponseStatus.SALE.ToString())
            {
                await EnsureExpressPaymentAndShipping(cart, payment, orderId);
                payment.Status = PaymentStatus.Processed.ToString();
                payment.TransactionID = statusResponse.TransactionInfo.TransactionId;
                OrderNoteHelper.AddNoteAndSaveChanges(cart, payment, "Initiate",
                    $"Payment with order id: {orderId} successfully initiated with {statusResponse?.TransactionInfo?.Status}");

                var loadOrCreatePurchaseOrderResponse = await _vippsOrderCreator.LoadOrCreatePurchaseOrder(cart, orderId);
                if (loadOrCreatePurchaseOrderResponse.PurchaseOrder != null && loadOrCreatePurchaseOrderResponse.PurchaseOrderCreated)
                {
                    return new ProcessAuthorizationResponse
                    {
                        PurchaseOrder = loadOrCreatePurchaseOrderResponse.PurchaseOrder,
                        Processed = true
                    };
                }

                CancelPaymentHelper.CancelPayment(cart, payment);
                return new ProcessAuthorizationResponse
                {
                    Processed = false,
                    PaymentType = paymentType,
                    ProcessAuthorizationResponseError = new ProcessAuthorizationResponseError
                    {
                        ErrorMessage = loadOrCreatePurchaseOrderResponse.ErrorMessage,
                        ProcessAuthorizationErrorType = ProcessAuthorizationErrorType.ORDERVALIDATIONERROR
                    }
                };
            }

            //No error message. Transaction cancelled by user
            if (statusResponse?.TransactionInfo?.Status == VippsStatusResponseStatus.CANCEL.ToString())
            {
                return new ProcessAuthorizationResponse
                {
                    Processed = false,
                    PaymentType = paymentType
                };
            }

            payment.Status = PaymentStatus.Failed.ToString();
            OrderNoteHelper.AddNoteAndSaveChanges(cart, payment, "Initiate",
                $"Payment with order id: {orderId} failed to initiate. Status: {statusResponse?.TransactionInfo?.Status}");

            return new ProcessAuthorizationResponse
            {
                PaymentType = paymentType,
                Processed = false,
                ProcessAuthorizationResponseError = new ProcessAuthorizationResponseError
                {
                    ProcessAuthorizationErrorType = ProcessAuthorizationErrorType.INITIATEFAILED,
                    VippsStatusResponseStatus = Enum.TryParse<VippsStatusResponseStatus>(statusResponse?.TransactionInfo?.Status, out var status)
                        ? status
                        : VippsStatusResponseStatus.FAILED
                }
            };
        }

        public virtual async Task<DetailsResponse> GetOrderDetailsAsync(string orderId)
        {
            var paymentMethod = PaymentHelper.GetVippsPaymentMethodDto();
            var configuration = paymentMethod.GetVippsConfiguration();
            var serviceApi = _vippsServiceApiFactory.Create(configuration);

            try
            {
                return await serviceApi.Details(orderId).ConfigureAwait(false);
            }

            catch (ApiException apiException)
            {
                var errorMessage = GetErrorMessage(apiException);
                _logger.Log(Level.Error, $"Error getting payment details. Error message: {errorMessage}");
            }

            catch (Exception ex)
            {
                _logger.Error($"Error getting payment details. Exception: {ex.Message} {ex.StackTrace}");
            }

            return null;
        }

        public virtual async Task<StatusResponse> GetOrderStatusAsync(string orderId)
        {
            var paymentMethod = PaymentHelper.GetVippsPaymentMethodDto();
            var configuration = paymentMethod.GetVippsConfiguration();
            var serviceApi = _vippsServiceApiFactory.Create(configuration);

            try
            {
                return await serviceApi.Status(orderId).ConfigureAwait(false);
            }

            catch (ApiException apiException)
            {
                var errorMessage = GetErrorMessage(apiException);
                _logger.Log(Level.Error, $"Error getting order status. Error message: {errorMessage}");
            }

            catch (Exception ex)
            {
                _logger.Error($"Error getting order status. Exception: {ex.Message} {ex.StackTrace}");
            }

            return null;
        }

        #endregion

        #region private

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

        private static IPayment GetPayment(ICart cart)
        {
            if (cart.Forms == null || cart.Forms.Count == 0 || cart.GetFirstForm().Payments == null ||
                cart.GetFirstForm().Payments.Count == 0)
            {
                return null;
            }

            var payments = cart.GetFirstForm().Payments.Where(p => p.Status != PaymentStatus.Failed.ToString()).ToList();

            return payments.Any()
                ? payments.FirstOrDefault(x => x.TransactionType.Equals(TransactionType.Authorization.ToString()))
                : null;
        }

        private async Task EnsureExpressPaymentAndShipping(ICart cart, IPayment payment, string orderId)
        {
            if (cart.GetFirstShipment().ShippingMethodId == default(Guid) ||
                cart.GetFirstShipment().ShippingAddress == null ||
                payment?.BillingAddress == null)
            {
                var details = await GetOrderDetailsAsync(orderId);

                EnsureShipping(cart, details);
                EnsureBillingAddress(payment, cart, details);

                if (payment != null)
                {
                    payment.TransactionID = orderId;
                }
                _orderRepository.Save(cart);
            }
        }

        private VippsPaymentType GetVippsPaymentType(ICart cart)
        {
            if (Enum.TryParse<VippsPaymentType>(cart.Properties[VippsConstants.VippsPaymentTypeField]?.ToString(),
                out var paymentType))
            {
                return paymentType;
            }

            return VippsPaymentType.CHECKOUT;
        }

        private string GetErrorMessage(ApiException apiException)
        {
            if (apiException.HasContent)
            {
                try
                {
                    var errorResponses = JsonConvert.DeserializeObject<ErrorResponse[]>(apiException.Content);
                    var errorMessage = string.Join(". ",
                        errorResponses.Where(x => !string.IsNullOrEmpty(x?.ErrorMessage)).Select(x => x.ErrorMessage));

                    return errorMessage;
                }

                catch (Exception ex)
                {
                    _logger.Log(Level.Warning, $"Error deserializing error message. Content: {apiException?.Content}. Exception: {ex.Message}");
                }
            }

            return apiException.Message;
        }

        #endregion
    }
}