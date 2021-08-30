using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Vipps.Initialization;

namespace EPiServer.Reference.Commerce.Site.Infrastructure
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    internal class VippsPollingInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            PollingInitialization.Initialize(context);
        }

        public void Uninitialize(InitializationEngine context)
        {

        }
    }
}