using Mediachase.Commerce;

namespace Vipps
{
    public interface IVippsConfigurationLoader
    {
        VippsConfiguration GetConfiguration(MarketId marketId);

        VippsConfiguration GetConfiguration(MarketId marketId, string languageId);
    }
}