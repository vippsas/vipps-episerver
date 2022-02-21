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
        private readonly IVippsOrderProcessor _vippsOrderProcessor;
        private readonly IVippsOrderSynchronizer _vippsOrderSynchronizer;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(VippsPollingService));
        private bool _start = true;
        private bool _running;

        public VippsPollingService(
            PollingEntityDbContext pollingEntityContext,
            IVippsService vippsService,
            IVippsOrderProcessor vippsOrderProcessor,
            IVippsOrderSynchronizer vippsOrderSynchronizer)
        {
            _pollingEntityContext = pollingEntityContext;
            _vippsService = vippsService;
            _vippsOrderProcessor = vippsOrderProcessor;
            _vippsOrderSynchronizer = vippsOrderSynchronizer;
        }

        public void Start(string orderId, IOrderGroup orderGroup)
        {
            var vippsPollingEntity = new VippsPollingEntity
            {
                // orderGroup.Created is in UTC
                Created = orderGroup.Created,
                CartName = orderGroup.Name,
                ContactId = orderGroup.CustomerId,
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
                    var pollingEntities = _pollingEntityContext.PollingEntities.Where(x => x.InstanceId == instanceId);

                    if (!pollingEntities.Any())
                    {
                        Stop();
                    }

                    foreach (var entity in pollingEntities)
                    {
                        // since entity.Created comes from orderGroup.Created which is in UTC
                        // UTC should be used to check this time
                        if (entity.Created.AddMinutes(-10) < DateTime.UtcNow)
                        {
                            _logger.Debug($"Running polling for entity with orderId: {entity.OrderId} and instanceId: {entity.InstanceId} on instance {instanceId}");
                            var result = await _vippsOrderProcessor.FetchAndProcessOrderDetailsAsync(entity.OrderId,
                                entity.ContactId, entity.MarketId, entity.CartName);
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

                var hasChanges = false;
                foreach (var entityToRemove in pollingEntitiesToRemove)
                {
                    _pollingEntityContext.PollingEntities.Remove(entityToRemove);
                    hasChanges = true;
                }

                if (hasChanges)
                    _pollingEntityContext.SaveChangesDatabaseWins();

                _running = false;
            }
        }
    }
}
