using System;
using System.Linq;
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

        public async Task<ProcessOrderResponse> ProcessOrderDetails(DetailsResponse detailsResponse, string orderId, Guid contactId, string marketId, string cartName)
        {
            var lastTransaction = detailsResponse.TransactionLogHistory.OrderByDescending(x => x.TimeStamp).FirstOrDefault();
            return await ProcessOrder(detailsResponse, lastTransaction, orderId, contactId, marketId, cartName);

        }

        private async Task<ProcessOrderResponse> ProcessOrder(IVippsUserDetails vippsUserDetails, IVippsPaymentDetails paymentDetails, string orderId, Guid contactId, string marketId, string cartName)
        {
            var readLock = _synchronizer.Get(orderId);

            try
            {
                await readLock.WaitAsync();
                var purchaseOrder = _vippsService.GetPurchaseOrderByOrderId(orderId);
                if (purchaseOrder != null)
                {
                    return new ProcessOrderResponse
                    {
                        PurchaseOrder = purchaseOrder
                    };
                }

                var cart = _vippsService.GetCartByContactId(contactId, marketId, cartName);
                if (cart == null)
                {
                    _logger.Warning($"No cart found for vipps order id {orderId}");
                    return new ProcessOrderResponse
                    {
                        ProcessResponseErrorType = ProcessResponseErrorType.NOCARTFOUND,
                        ErrorMessage = $"No cart found for vipps order id {orderId}"
                    };
                }

                var payment = cart.GetFirstForm().Payments.FirstOrDefault(x =>
                    x.IsVippsPayment() && x.TransactionID == orderId &&
                    x.TransactionType.Equals(TransactionType.Authorization.ToString()));


                if (TransactionSuccess(paymentDetails))
                {
                    return await HandleSuccess(cart, payment, paymentDetails, vippsUserDetails, orderId);
                }

                if (TransactionCancelled(paymentDetails))
                {
                    return HandleCancelled(cart, payment, paymentDetails, orderId);
                }

                if (TransactionFailed(paymentDetails))
                {
                    return HandleFailed(cart, payment, paymentDetails, orderId);
                }

                return new ProcessOrderResponse
                {
                    ProcessResponseErrorType = ProcessResponseErrorType.OTHER,
                    ErrorMessage = $"No action taken on order id: {orderId}."
                };
            }

            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                return new ProcessOrderResponse
                {
                    ErrorMessage = ex.Message,
                    ProcessResponseErrorType = ProcessResponseErrorType.EXCEPTION
                };
            }

            finally
            {
                readLock.Release();

                if (readLock.CurrentCount > 0)
                    _synchronizer.Remove(orderId);
            }
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

        private async Task<ProcessOrderResponse> HandleSuccess(ICart cart, IPayment payment, IVippsPaymentDetails paymentDetails, IVippsUserDetails userDetails, string orderId)
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

            var loadOrCreatePurchaseOrderResponse = await CreatePurchaseOrder(cart);
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

        public async Task<ProcessOrderResponse> ProcessPaymentCallback(PaymentCallback paymentCallback, string orderId, string contactId, string marketId,
            string cartName)
        {
            return await ProcessOrder(paymentCallback, paymentCallback.TransactionInfo, orderId, Guid.Parse(contactId), marketId,
                cartName);
        }

        public virtual async Task<ProcessOrderResponse> CreatePurchaseOrder(ICart cart)
        {
            try
            {
                _logger.Information($"Creating PurchaseOrder for orderId {cart.Properties[VippsConstants.VippsOrderIdField]}");

                //Add your order validation here
                var orderReference = _orderRepository.SaveAsPurchaseOrder(cart);
                var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);
                _orderRepository.Delete(cart.OrderLink);
                return new ProcessOrderResponse
                {
                    PurchaseOrder = purchaseOrder
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return new ProcessOrderResponse
                {
                    ErrorMessage = ex.Message,
                    ProcessResponseErrorType = ProcessResponseErrorType.EXCEPTION
                };
            }
        }

        public void EnsureExpressPaymentAndShipping(ICart cart, IPayment payment, IVippsPaymentDetails paymentDetails, IVippsUserDetails userDetails)
        {
            EnsureShipping(cart, userDetails);
            EnsurePayment(payment, cart, paymentDetails, userDetails);
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
