using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Internal;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Search;

namespace Vipps.Services
{
    [ServiceConfiguration(typeof(IVippsService))]
    public class VippsService : IVippsService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly SerializableCartProvider _serializableCartProvider;

        public VippsService(IOrderRepository orderRepository, SerializableCartProvider serializableCartProvider)
        {
            _orderRepository = orderRepository;
            _serializableCartProvider = serializableCartProvider;
        }

        public virtual ICart GetCartByContactId(string contactId, string marketId, string orderId)
        {
            return GetCartByContactId(Guid.Parse(contactId), marketId, orderId);
        }

        public virtual ICart GetCartByContactId(Guid contactId, string marketId, string orderId)
        {
            var defaultCart = _orderRepository.LoadCart<ICart>(contactId, VippsConstants.VippsDefaultCartName, marketId);
            if (defaultCart != null && defaultCart.Properties[VippsConstants.VippsOrderIdField]?.ToString() == orderId)
            {
                return defaultCart;
            }

            var singleProductCart = _orderRepository.LoadCart<ICart>(contactId, VippsConstants.VippsSingleProductCart, marketId);
            if (singleProductCart != null && singleProductCart.Properties[VippsConstants.VippsOrderIdField]?.ToString() == orderId)
            {
                return singleProductCart;
            }

            return GetCartByOrderId(orderId);
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

        private ICart GetCartByOrderId(string orderId)
        {
            //Last resort find cart by sql query
            var connectionString = ConfigurationManager.ConnectionStrings["EcfSqlConnection"];
            var sqlString = $"Select CartId FROM [SerializableCart] WHERE Data Like '%\"$value\":\"{orderId}\"%' And Data Like '%{VippsConstants.VippsOrderIdField}%'";

            using (var sqlConnection = new SqlConnection(connectionString.ConnectionString))
            {
                var cmd = new SqlCommand(sqlString, sqlConnection);
                sqlConnection.Open();

                var value = cmd.ExecuteScalar();

                if (value != null)
                {
                    var cartId = (int)value;
                    return _serializableCartProvider.Load(cartId);
                }
            }

            return null;
        }
    }
}
