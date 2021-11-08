using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Vipps.Extensions;
using Vipps.Helpers;
using Vipps.Models;
using Vipps.Models.Partials;
using Vipps.Models.ResponseModels;

namespace Vipps.Services
{
    [ServiceConfiguration(typeof(IVippsOrderProcessor))]
    public class DefaultVippsOrderProcessor : IVippsOrderProcessor
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IVippsService _vippsService;
        private readonly IVippsOrderSynchronizer _synchronizer;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(DefaultVippsOrderProcessor));

        public DefaultVippsOrderProcessor(
            IOrderRepository orderRepository,
            IVippsService vippsService,
            IVippsOrderSynchronizer synchronizer)
        {
            _orderRepository = orderRepository;
            _vippsService = vippsService;
            _synchronizer = synchronizer;
        }

        public async Task<ProcessOrderResponse> FetchAndProcessOrderDetails(string orderId, Guid contactId, string marketId, string cartName)
        {
            if (_logger.IsDebugEnabled())
                _logger.Debug($"Starting processing of vipps order: {orderId}.");

            var lockInfo = GetLock(orderId, contactId, marketId, cartName);
            var result = await ProcessLockedAsync(lockInfo, () => CheckDependenciesThenProcessPaymentAsync(orderId, contactId, marketId, cartName));

            if (_logger.IsDebugEnabled())
                _logger.Debug($"Ending processing of vipps order: {orderId}.");

            return result;
        }

        // Note: This overload is obsolete
        public Task<ProcessOrderResponse> ProcessOrderDetails(DetailsResponse detailsResponse, string orderId, Guid contactId, string marketId, string cartName)
        {
            var lastTransaction = GetLastTransaction(detailsResponse);
            var result = ProcessOrder(detailsResponse, lastTransaction, orderId, contactId, marketId, cartName);

            return Task.FromResult(result);
        }

        public ProcessOrderResponse ProcessOrderDetails(DetailsResponse detailsResponse, string orderId, ICart cart)
        {
            var lastTransaction = GetLastTransaction(detailsResponse);
            return ProcessOrder(detailsResponse, lastTransaction, orderId, cart);
        }

        public Task<ProcessOrderResponse> ProcessPaymentCallback(PaymentCallback paymentCallback, string orderId, string contactId, string marketId,
            string cartName)
        {
            var result = ProcessOrder(paymentCallback, paymentCallback.TransactionInfo, orderId, Guid.Parse(contactId), marketId, cartName);
            return Task.FromResult(result);
        }

        public virtual Task<ProcessOrderResponse> CreatePurchaseOrder(ICart cart)
        {
            return Task.FromResult(ConvertToPurchaseOrder(cart));
        }

        protected virtual ProcessOrderResponse ConvertToPurchaseOrder(ICart cart)
        {
            ProcessOrderResponse result;

            try
            {
                if (_logger.IsInformationEnabled())
                    _logger.Information($"Creating PurchaseOrder for orderId {cart.Properties[VippsConstants.VippsOrderIdField]}");

                //Add your order validation here
                var orderReference = _orderRepository.SaveAsPurchaseOrder(cart);
                var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);

                _orderRepository.Delete(cart.OrderLink);

                result = new ProcessOrderResponse
                {
                    PurchaseOrder = purchaseOrder
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);

                result = new ProcessOrderResponse
                {
                    ErrorMessage = ex.Message,
                    ProcessResponseErrorType = ProcessResponseErrorType.EXCEPTION,
                };
            }

            return result;
        }

        private ProcessOrderResponse ProcessOrder(IVippsUserDetails vippsUserDetails, IVippsPaymentDetails paymentDetails, string orderId, Guid contactId, string marketId, string cartName)
        {
            var lockInfo = GetLock(orderId, contactId, marketId, cartName);
            return ProcessLocked(lockInfo, () => CheckDependenciesThenProcessPayment(vippsUserDetails, paymentDetails, orderId, contactId, marketId, cartName));
        }

        private ProcessOrderResponse ProcessOrder(IVippsUserDetails vippsUserDetails, IVippsPaymentDetails paymentDetails, string orderId, ICart cart)
        {
            var lockInfo = GetLock(orderId, cart);
            return ProcessLocked(lockInfo, () => ProcessPayment(vippsUserDetails, paymentDetails, orderId, cart));
        }

        private async Task<ProcessOrderResponse> CheckDependenciesThenProcessPaymentAsync(string orderId, Guid contactId, string marketId, string cartName)
        {
            var purchaseOrder = _vippsService.GetPurchaseOrderByOrderId(orderId);
            if (purchaseOrder != null)
            {
                return new ProcessOrderResponse
                {
                    PurchaseOrder = purchaseOrder
                };
            }

            var cart = _vippsService.GetCartByContactId(contactId, marketId, cartName);
            var errorResponse = ValidateCart(orderId, cart);
            if (errorResponse != null)
                return errorResponse;

            cart.SetOrderProcessing(true);

            _orderRepository.Save(cart);

            var detailsResponse = await _vippsService.GetOrderDetailsAsync(orderId, marketId);
            var lastTransaction = GetLastTransaction(detailsResponse);

            return ProcessPayment(detailsResponse, lastTransaction, orderId, cart);
        }

        private ProcessOrderResponse CheckDependenciesThenProcessPayment(IVippsUserDetails vippsUserDetails, IVippsPaymentDetails paymentDetails, string orderId, Guid contactId, string marketId, string cartName)
        {
            var purchaseOrder = _vippsService.GetPurchaseOrderByOrderId(orderId);
            if (purchaseOrder != null)
            {
                return new ProcessOrderResponse
                {
                    PurchaseOrder = purchaseOrder
                };
            }

            return GetCartThenProcessPayment(vippsUserDetails, paymentDetails, orderId, contactId, marketId, cartName);
        }

        private ProcessOrderResponse GetCartThenProcessPayment(IVippsUserDetails vippsUserDetails, IVippsPaymentDetails paymentDetails, string orderId, Guid contactId, string marketId, string cartName)
        {
            var cart = _vippsService.GetCartByContactId(contactId, marketId, cartName);
            var errorResponse = ValidateCart(orderId, cart);
            if (errorResponse != null)
                return errorResponse;

            cart.SetOrderProcessing(true);

            _orderRepository.Save(cart);

            return ProcessPayment(vippsUserDetails, paymentDetails, orderId, cart);
        }

        private ProcessOrderResponse ProcessPayment(IVippsUserDetails vippsUserDetails, IVippsPaymentDetails paymentDetails, string orderId, ICart cart)
        {
            ProcessOrderResponse response;

            var payment = cart.GetFirstPayment(x =>
                       x.IsVippsPayment() &&
                       x.TransactionID == orderId &&
                       x.TransactionType.Equals(nameof(TransactionType.Authorization)));

            if (TransactionSuccess(paymentDetails))
            {
                response = HandleSuccess(cart, payment, paymentDetails, vippsUserDetails, orderId);
            }
            else if (TransactionCancelled(paymentDetails))
            {
                response = HandleCancelled(cart, payment, paymentDetails, orderId);
            }
            else if (TransactionFailed(paymentDetails))
            {
                response = HandleFailed(cart, payment, paymentDetails, orderId);
            }
            else
            {
                response = new ProcessOrderResponse
                {
                    ProcessResponseErrorType = ProcessResponseErrorType.OTHER,
                    ErrorMessage = $"No action taken on order id: {orderId}."
                };
            }

            return EnsurePaymentType(response, cart);
        }

        private IVippsPaymentDetails GetLastTransaction(DetailsResponse detailsResponse)
        {
            return detailsResponse.TransactionLogHistory.OrderByDescending(x => x.TimeStamp)
                                                        .FirstOrDefault();
        }

        private ProcessOrderResponse ValidateCart(string orderId, ICart cart)
        {
            if (cart == null)
            {
                _logger.Warning($"No cart found for vipps order id {orderId}");

                return new ProcessOrderResponse
                {
                    PaymentType = VippsPaymentType.UNKNOWN,
                    ProcessResponseErrorType = ProcessResponseErrorType.NOCARTFOUND,
                    ErrorMessage = $"No cart found for vipps order id {orderId}"
                };
            }

            if (cart.IsProcessingOrder())
            {
                _logger.Warning($"Vipps order {orderId} is already processing");

                return new ProcessOrderResponse
                {
                    PaymentType = cart.GetVippsPaymentType(),
                    ProcessResponseErrorType = ProcessResponseErrorType.OTHER,
                    ErrorMessage = $"Order is already processing"
                };
            }

            return null;
        }

        private ProcessOrderResponse HandleFailed(ICart cart, IPayment payment, IVippsPaymentDetails paymentDetails, string orderId)
        {
            if (payment != null)
            {
                payment.Status = PaymentStatus.Failed.ToString();
                AddNote(cart, payment, orderId, paymentDetails);
            }

            else
            {
                AddNote(cart, orderId, paymentDetails);
            }
            
            return new ProcessOrderResponse
            {
                ProcessResponseErrorType = ProcessResponseErrorType.FAILED
            };
        }

        private ProcessOrderResponse HandleCancelled(ICart cart, IPayment payment, IVippsPaymentDetails paymentDetails, string orderId)
        {
            if (payment == null)
            {
                AddNote(cart, orderId, paymentDetails);
                return new ProcessOrderResponse
                {
                    ProcessResponseErrorType = ProcessResponseErrorType.NONE
                };
            }

            return new ProcessOrderResponse
            {
                ProcessResponseErrorType = ProcessResponseErrorType.NONE
            };
        }

        private ProcessOrderResponse HandleSuccess(ICart cart, IPayment payment, IVippsPaymentDetails paymentDetails, IVippsUserDetails userDetails, string orderId)
        {
            if (payment == null)
            {
                OrderNoteHelper.AddNoteAndSaveChanges(cart, "Cancel", $"No vipps payment found for vipps order id {orderId}. Canceling payment");
                PaymentHelper.CancelPayment(cart, paymentDetails.Amount, orderId);

                return new ProcessOrderResponse
                {
                    ErrorMessage = $"No vipps payment found for vipps order id {orderId}.",
                    ProcessResponseErrorType = ProcessResponseErrorType.NOVIPPSPAYMENTINCART
                };
            }

            EnsureExpressPaymentAndShipping(cart, payment, paymentDetails, userDetails);
            payment.Status = PaymentStatus.Processed.ToString();
            AddNote(cart, payment, orderId, paymentDetails);

            var loadOrCreatePurchaseOrderResponse = ConvertToPurchaseOrder(cart);
            if (loadOrCreatePurchaseOrderResponse.PurchaseOrder != null)
            {
                return loadOrCreatePurchaseOrderResponse;
            }

            PaymentHelper.CancelPayment(cart, payment);
            return loadOrCreatePurchaseOrderResponse;
        }

        private bool TransactionFailed(IVippsPaymentDetails paymentDetails)
        {
            if (paymentDetails is TransactionLogHistory transactionLogHistory)
            {
                return transactionLogHistory.Operation == VippsDetailsResponseOperation.RESERVE.ToString() && !transactionLogHistory.OperationSuccess
                        || transactionLogHistory.Operation == VippsDetailsResponseOperation.SALE.ToString() && !transactionLogHistory.OperationSuccess;
            }

            if (paymentDetails is TransactionInfo transactionInfo)
            {
                return transactionInfo.Status == VippsCallbackStatus.SALE_FAILED.ToString() 
                        || transactionInfo.Status == VippsCallbackStatus.RESERVE_FAILED.ToString() 
                        || transactionInfo.Status == VippsCallbackStatus.REJECTED.ToString();
            }

            throw new InvalidCastException(nameof(paymentDetails));
        }

        private bool TransactionCancelled(IVippsPaymentDetails paymentDetails)
        {
            if (paymentDetails is TransactionLogHistory transactionLogHistory)
            {
                return transactionLogHistory.Operation == VippsDetailsResponseOperation.CANCEL.ToString();
            }

            if (paymentDetails is TransactionInfo transactionInfo)
            {
                return transactionInfo.Status == VippsCallbackStatus.CANCELLED.ToString();
            }

            throw new InvalidCastException(nameof(paymentDetails));
        }

        private void AddNote(ICart cart, IPayment payment, string orderId, IVippsPaymentDetails paymentDetails)
        {
            if (paymentDetails is TransactionLogHistory transactionLogHistory)
            {
                OrderNoteHelper.AddNoteAndSaveChanges(cart, payment, transactionLogHistory.Operation,
                    $"Payment with order id: {orderId}. Operation: {transactionLogHistory.Operation} Success: {transactionLogHistory.OperationSuccess}");
                return;
            }

            if (paymentDetails is TransactionInfo transactionInfo)
            {
                OrderNoteHelper.AddNoteAndSaveChanges(cart, payment, transactionInfo.Status,
                    $"Payment with order id: {orderId}. Status: {transactionInfo.Status}");
                return;
            }

            throw new InvalidCastException(nameof(paymentDetails));
        }

        private void AddNote(ICart cart, string orderId, IVippsPaymentDetails paymentDetails)
        {
            if (paymentDetails is TransactionLogHistory transactionLogHistory)
            {
                OrderNoteHelper.AddNoteAndSaveChanges(cart, transactionLogHistory.Operation,
                    $"Payment with order id: {orderId}. Operation: {transactionLogHistory.Operation} Success: {transactionLogHistory.OperationSuccess}");
                return;
            }

            if (paymentDetails is TransactionInfo transactionInfo)
            {
                OrderNoteHelper.AddNoteAndSaveChanges(cart, transactionInfo.Status,
                    $"Payment with order id: {orderId}. Status: {transactionInfo.Status}");
                return;
            }

            throw new InvalidCastException(nameof(paymentDetails));
        }

        private bool TransactionSuccess(IVippsPaymentDetails paymentDetails)
        {
            if (paymentDetails is TransactionLogHistory transactionLogHistory)
            {
                return transactionLogHistory?.Operation == VippsDetailsResponseOperation.RESERVE.ToString() &&
                        transactionLogHistory.OperationSuccess ||
                        transactionLogHistory?.Operation == VippsDetailsResponseOperation.SALE.ToString() &&
                        transactionLogHistory.OperationSuccess;
            }

            if (paymentDetails is TransactionInfo transactionInfo)
            {
                return transactionInfo.Status == VippsCallbackStatus.RESERVED.ToString() 
                       || transactionInfo.Status == VippsCallbackStatus.SALE.ToString()
                       || transactionInfo.Status == VippsCallbackStatus.RESERVE.ToString();
            }

            throw new InvalidCastException(nameof(paymentDetails));
        }

        private ProcessOrderResponse ProcessLocked(ProcessLockInformation lockInfo, Func<ProcessOrderResponse> method)
        {
            ProcessOrderResponse response;

            var readLock = _synchronizer.Get(lockInfo.OrderId);

            try
            {
                readLock.Wait();
                response = method();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                response = GetErrorResponse(ex);
            }
            finally
            {
                TryReleaseLock(lockInfo, readLock);
            }

            return TryEnsureCartNotProcessing(lockInfo, response);
        }

        private async Task<ProcessOrderResponse> ProcessLockedAsync(ProcessLockInformation lockInfo, Func<Task<ProcessOrderResponse>> method)
        {
            ProcessOrderResponse response;

            var readLock = _synchronizer.Get(lockInfo.OrderId);

            try
            {
                await readLock.WaitAsync();
                response = await method();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                response = GetErrorResponse(ex);
            }
            finally
            {
                TryReleaseLock(lockInfo, readLock);
            }

            return TryEnsureCartNotProcessing(lockInfo, response);
        }
        
        private ProcessLockInformation GetLock(string orderId, Guid contactId, string marketId, string cartName)
        {
            return new ProcessLockInformation(orderId, contactId, marketId, cartName);
        }

        private ProcessLockInformation GetLock(string orderId, ICart cart)
        {
            return new ProcessLockInformation(orderId, cart.OrderLink.OrderGroupId);
        }

        private bool TryReleaseLock(ProcessLockInformation lockInfo, SemaphoreSlim readLock)
        {
            try
            {
                readLock.Release();

                if (readLock.CurrentCount > 0)
                    _synchronizer.Remove(lockInfo.OrderId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                return false;
            }
        }

        private ProcessOrderResponse GetErrorResponse(Exception ex)
        {
            return new ProcessOrderResponse
            {
                ErrorMessage = ex.Message,
                PaymentType = VippsPaymentType.UNKNOWN,
                ProcessResponseErrorType = ProcessResponseErrorType.EXCEPTION
            };
        }

        private void SetCartNotProcessing(string orderId, ICart cart)
        {
            var processing = cart.IsProcessingOrder();
            if (processing == false)
                return;

            _logger.Warning($"Resetting processing state of cart for vipps order {orderId}.");

            cart.SetOrderProcessing(false);

            _orderRepository.Save(cart);
        }

        private ProcessOrderResponse TryEnsureCartNotProcessing(ProcessLockInformation lockInfo, ProcessOrderResponse response)
        {

            try
            {
                EnsureCartNotProcessing(lockInfo, response);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
            }

            return response;
        }

        private void EnsureCartNotProcessing(ProcessLockInformation lockInfo, ProcessOrderResponse response)
        {
            if (response.PurchaseOrder != null) return;

            ICart cart;

            if (lockInfo.OrderGroupId.HasValue)
            {
                cart = _orderRepository.Load<ICart>(lockInfo.OrderGroupId.Value);
            }
            else
            {
                cart = _vippsService.GetCartByContactId(lockInfo.ContactId.Value, lockInfo.MarketId, lockInfo.CartName);
            }

            SetCartNotProcessing(lockInfo.OrderId, cart);
        }

        private void EnsureExpressPaymentAndShipping(ICart cart, IPayment payment, IVippsPaymentDetails paymentDetails, IVippsUserDetails userDetails)
        {
            EnsureShipping(cart, userDetails);
            EnsurePayment(payment, cart, paymentDetails, userDetails);
        }

        private static ProcessOrderResponse EnsurePaymentType(ProcessOrderResponse response, ICart cart)
        {
            if (response == null) return response;
            if (response.PurchaseOrder != null) return response;

            response.PaymentType = cart.GetVippsPaymentType();

            return response;
        }

        private static void EnsurePayment(IPayment payment, ICart cart, IVippsPaymentDetails paymentDetails, IVippsUserDetails details)
        {
            if (string.IsNullOrEmpty(payment.BillingAddress?.Id))
            {
                payment.BillingAddress =
                    AddressHelper.UserDetailsAndShippingDetailsToOrderAddress(details.UserDetails,
                        details.ShippingDetails, cart);
                payment.Amount = paymentDetails.Amount.FormatAmountFromVipps();
            }
        }

        private static void EnsureShipping(ICart cart, IVippsUserDetails details)
        {
            var shipment = cart.GetFirstShipment();
            if (details?.ShippingDetails?.ShippingMethodId != null &&
                (shipment.ShippingMethodId == default(Guid) || shipment.ShippingMethodId.ToString() != details.ShippingDetails.ShippingMethodId))
            {
                if (details?.ShippingDetails?.ShippingMethodId != null)
                {
                    shipment.ShippingMethodId = new Guid(details.ShippingDetails.ShippingMethodId);
                }
            }

            if (details?.ShippingDetails != null && details.UserDetails != null)
            {
                shipment.ShippingAddress =
                    AddressHelper.UserDetailsAndShippingDetailsToOrderAddress(details.UserDetails,
                        details.ShippingDetails, cart);
            }
        }
    }
}
