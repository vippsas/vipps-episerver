using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Vipps.Extensions;
using Vipps.Models;
using Vipps.Services;

namespace Vipps.Polling
{
    [ServiceConfiguration(typeof(IVippsPollingService))]
    public class VippsPollingService : IVippsPollingService
    {
        private readonly PollingEntityDbContext _pollingEntityContext;
        private readonly IVippsService _vippsService;
        private readonly IVippsOrderProcessor _vippsOrderCreator;
        private readonly IVippsOrderSynchronizer _vippsOrderSynchronizer;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(VippsPollingService));
        private bool _start = true;
        private bool _running;

        public VippsPollingService(
            PollingEntityDbContext pollingEntityContext,
            IVippsService vippsService,
            IVippsOrderProcessor vippsOrderCreator,
            IVippsOrderSynchronizer vippsOrderSynchronizer)
        {
            _pollingEntityContext = pollingEntityContext;
            _vippsService = vippsService;
            _vippsOrderCreator = vippsOrderCreator;
            _vippsOrderSynchronizer = vippsOrderSynchronizer;
        }

        public void Start(string orderId, IOrderGroup orderGroup)
        {
            var vippsPollingEntity = new VippsPollingEntity
            {
                CartName = orderGroup.Name,
                ContactId = orderGroup.CustomerId,
                Created = orderGroup.Created,
                MarketId = orderGroup.MarketId.Value,
                OrderId = orderId,
                InstanceId = _vippsOrderSynchronizer.GetInstanceId()
            };

            _pollingEntityContext.PollingEntities.Add(vippsPollingEntity);
            _pollingEntityContext.SaveChangesDatabaseWins();
            _start = true;
        }

        public void Stop()
        {
            _start = false;
        }

        public async Task Run()
        {
            if (_start && !_running)
            {
                _running = true;
                var pollingEntitiesToRemove = new List<VippsPollingEntity>();

                try
                {
                    var instanceId = _vippsOrderSynchronizer.GetInstanceId();
                    var pollingEntities = _pollingEntityContext.PollingEntities.ToList().Where(x => x.InstanceId == instanceId);

                    if (!pollingEntities.Any())
                    {
                        Stop();
                    }

                    foreach (var entity in pollingEntities)
                    {
                        if (entity.Created.AddMinutes(-10) < DateTime.Now)
                        {
                            _logger.Debug($"Running polling for entity with orderId: {entity.OrderId} and instanceId: {entity.InstanceId} on instance {instanceId}");
                            var orderDetails = await _vippsService.GetOrderDetailsAsync(entity.OrderId, entity.MarketId);
                            if (orderDetails == null)
                            {
                                _logger.Warning($"No order details for vipps order {entity.OrderId}");
                                pollingEntitiesToRemove.Add(entity);
                            }

                            var result = await _vippsOrderCreator.ProcessOrderDetails(orderDetails, entity.OrderId, entity.ContactId,
                                entity.MarketId, entity.CartName);
                            if (result.PurchaseOrder != null || result.ProcessResponseErrorType != ProcessResponseErrorType.OTHER)
                            {
                                pollingEntitiesToRemove.Add(entity);
                            }
                        }

                        else
                        {
                            _logger.Information($"Vipps payment with order id {entity.OrderId} reached max recount. Stopping polling.");
                            pollingEntitiesToRemove.Add(entity);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message, ex);
                }

                foreach (var entityToRemove in pollingEntitiesToRemove)
                {
                    _pollingEntityContext.PollingEntities.Remove(entityToRemove);
                }

                _pollingEntityContext.SaveChangesDatabaseWins();
                _running = false;
            }
        }
    }
}
