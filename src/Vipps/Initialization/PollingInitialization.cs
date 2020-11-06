using System.Configuration;
using System.Timers;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Vipps.Polling;

namespace Vipps.Initialization
{
    public static class PollingInitialization
    {
        private static Timer _timer = new Timer();
        private static IVippsPollingService _pollingService;

        public static void Initialize(InitializationEngine context)
        {
            _pollingService = context.Locate.Advanced.GetInstance<IVippsPollingService>();
            _timer.Interval = double.TryParse(ConfigurationManager.AppSettings["Vipps:PollingInterval"], out var pollingInterval) ? pollingInterval : 2000;
            _timer.Start();
            _timer.Elapsed += ExecutePolling;
        }

        public static void ExecutePolling(object sender, ElapsedEventArgs e)
        {
            _pollingService.Run();
        }


        public static void Uninitialize(InitializationEngine context)
        {

        }
    }
}