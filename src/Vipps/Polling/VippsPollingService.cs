using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPiServer.Logging;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
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
        private readonly SchedulerOptions _schedulerOptions;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(VippsPollingService));
        private bool _start = true;
        private bool _running;

        public VippsPollingService(PollingEntityDbContext pollingEntityContext,
            IVippsService vippsService,
            IVippsOrderProcessor vippsOrderCreator, 
            SchedulerOptions schedulerOptions)
        {
            _pollingEntityContext = pollingEntityContext;
            _vippsService = vippsService;
            _vippsOrderCreator = vippsOrderCreator;
            _schedulerOptions = schedulerOptions;
        }

        public void Start(VippsPollingEntity vippsPollingEntity)
        {
            _pollingEntityContext.PollingEntities.Add(vippsPollingEntity);
            _pollingEntityContext.SaveChanges();
            _start = true;
        }

        public async Task Run()
        {
            if (_start && !_running && _schedulerOptions.Enabled)
            {
                _running = true;
                var pollingEntitiesToRemove = new List<VippsPollingEntity>();

                try
                {
                    var pollingEntities = _pollingEntityContext.PollingEntities.ToList();

                    if (!pollingEntities.Any())
                    {
                        _start = false;
                    }

                    foreach (var entity in pollingEntities)
                    {
                        if (entity.Created.AddMinutes(-10) < DateTime.Now)
                        {
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

                _pollingEntityContext.SaveChanges();
                _running = false;
            }
        }
    }
}
