using System;
using System.Linq;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Search;
using Refit;
using Vipps.Extensions;
using Vipps.Helpers;
using Vipps.Models.ResponseModels;

namespace Vipps.Services
{
    [ServiceConfiguration(typeof(IVippsService))]
    public class VippsService : VippsServiceBase, IVippsService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly VippsServiceApiFactory _vippsServiceApiFactory;
        private readonly IVippsConfigurationLoader _configurationLoader;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(VippsService));

        public VippsService(IOrderRepository orderRepository,
            VippsServiceApiFactory vippsServiceApiFactory,
            IVippsConfigurationLoader configurationLoader)
        {
            _orderRepository = orderRepository;
            _vippsServiceApiFactory = vippsServiceApiFactory;
            _configurationLoader = configurationLoader;
        }

        public virtual async Task<DetailsResponse> GetOrderDetailsAsync(string orderId, string marketId)
        {
            var configuration = _configurationLoader.GetConfiguration(marketId);
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

        public virtual async Task<StatusResponse> GetOrderStatusAsync(string orderId, string marketId)
        {
            var configuration = _configurationLoader.GetConfiguration(marketId);
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

        public virtual ICart GetCartByContactId(string contactId, string marketId, string cartName)
        {
            return GetCartByContactId(Guid.Parse(contactId), marketId, cartName);
        }

        public virtual ICart GetCartByContactId(Guid contactId, string marketId, string cartName)
        {
            return _orderRepository.LoadCart<ICart>(contactId, cartName, marketId);
        }

        public IPurchaseOrder GetPurchaseOrderByOrderId(string orderId)
        {
            OrderSearchOptions searchOptions = new OrderSearchOptions
            {
                CacheResults = false,
                StartingRecord = 0,
                RecordsToRetrieve = 1,
                Classes = new System.Collections.Specialized.StringCollection { "PurchaseOrder" },
                Namespace = "Mediachase.Commerce.Orders"
            };

            var parameters = new OrderSearchParameters();
            parameters.SqlMetaWhereClause = $"META.{VippsConstants.VippsOrderIdField} LIKE '{orderId}'";

            var purchaseOrder = OrderContext.Current.Search<PurchaseOrder>(parameters, searchOptions)?.FirstOrDefault();

            if (purchaseOrder != null)
            {
                return _orderRepository.Load<IPurchaseOrder>(purchaseOrder.OrderGroupId);
            }
            return null;
        }
    }
}
