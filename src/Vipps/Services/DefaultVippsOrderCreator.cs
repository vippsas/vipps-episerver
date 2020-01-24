using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Vipps.Extensions;
using Vipps.Models;

namespace Vipps.Services
{
    [ServiceConfiguration(typeof(IVippsOrderCreator))]
    public class DefaultVippsOrderCreator : IVippsOrderCreator
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IVippsService _vippsService;
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(DefaultVippsOrderCreator));

        public DefaultVippsOrderCreator(
            IOrderRepository orderRepository,
            IVippsService vippsService,
            IOrderGroupCalculator groupCalculator)
        {
            _orderRepository = orderRepository;
            _vippsService = vippsService;
            _orderGroupCalculator = groupCalculator;
        }

        public virtual async Task<LoadOrCreatePurchaseOrderResponse> LoadOrCreatePurchaseOrder(ICart cart, string orderId)
        {
            var mutex = new Mutex(false, orderId);
            mutex.WaitOne();
            try
            {
                var purchaseOrder = _vippsService.GetPurchaseOrderByOrderId(orderId);
                if (purchaseOrder != null)
                {
                    _logger.Information($"Found PurchaseOrder for orderId {orderId}");
                    return new LoadOrCreatePurchaseOrderResponse
                    {
                        PurchaseOrder = purchaseOrder
                    };
                }

                return await CreatePurchaseOrder(cart);
            }

            catch (Exception ex)
            {
                _logger.Error($"Error creating/loading PurchaseOrder for vipps orderId {orderId}. Exception {ex.Message}, {ex.StackTrace}");
                return new LoadOrCreatePurchaseOrderResponse
                {
                    ErrorMessage = ex.Message
                };
            }

            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public virtual async Task<LoadOrCreatePurchaseOrderResponse> CreatePurchaseOrder(ICart cart)
        {
            try
            {
                // Do your validation here or in your _orderService.CreateOrder()
                if (_orderGroupCalculator.GetTotal(cart).Amount !=
                    cart.GetFirstForm().Payments.FirstOrDefault(x => x.IsVippsPayment())?.Amount)
                {
                    return new LoadOrCreatePurchaseOrderResponse
                    {
                        ErrorMessage = "Wrong payment amount. Please try again",
                        PurchaseOrderCreated = false
                    };
                }

                _logger.Information($"Creating PurchaseOrder for orderId {cart.Properties[VippsConstants.VippsOrderIdField]}");
                var orderReference = _orderRepository.SaveAsPurchaseOrder(cart);
                var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);
                _orderRepository.Delete(cart.OrderLink);
                return new LoadOrCreatePurchaseOrderResponse
                {
                    PurchaseOrder = purchaseOrder,
                    PurchaseOrderCreated = true
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return new LoadOrCreatePurchaseOrderResponse
                {
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
