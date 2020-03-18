using System.Configuration;
using System.Timers;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Vipps.Polling;

namespace Vipps.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    internal class PollingInitialization : IInitializableModule
    {
        private Timer _timer = new Timer();
        private IVippsPollingService _pollingService;

        public void Initialize(InitializationEngine context)
        {
            _pollingService = context.Locate.Advanced.GetInstance<IVippsPollingService>();
            _timer.Interval = double.TryParse(ConfigurationManager.AppSettings["Vipps:PollingInterval"], out var pollingInterval) ? pollingInterval : 2000;
            _timer.Start();
            _timer.Elapsed += ExecutePolling;
        }

        private void ExecutePolling(object sender, ElapsedEventArgs e)
        {
            _pollingService.Run();
        }


        public void Uninitialize(InitializationEngine context)
        {

        }
    }
}