using System;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using Mediachase.BusinessFoundation.Core;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using Newtonsoft.Json;
using Vipps.Extensions;

namespace Vipps
{
    [ServiceConfiguration(typeof(IVippsConfigurationLoader))]
    public class DefaultVippsConfigurationLoader : IVippsConfigurationLoader
    {
        public VippsConfiguration GetConfiguration(MarketId marketId, string languageId)
        {
            var paymentMethod = PaymentManager.GetPaymentMethodBySystemName(
                VippsConstants.VippsSystemKeyword, languageId, returnInactive: true);
            if (paymentMethod == null)
            {
                throw new Exception(
                    $"PaymentMethod {VippsConstants.VippsSystemKeyword} is not configured for market {marketId} and language {ContentLanguage.PreferredCulture.Name}");
            }
            return GetVippsCheckoutConfiguration(paymentMethod, marketId);
        }

        public VippsConfiguration GetConfiguration(MarketId marketId)
        {
            return GetConfiguration(marketId, ContentLanguage.PreferredCulture.Name);
        }

        private static VippsConfiguration GetVippsCheckoutConfiguration(
            PaymentMethodDto paymentMethodDto, MarketId marketId)
        {
            var parameter = paymentMethodDto.GetParameter($"{marketId.Value}_{VippsConstants.VippsSerializedMarketOptions}", string.Empty);

            var configuration = JsonConvert.DeserializeObject<VippsConfiguration>(parameter);

            return configuration ?? new VippsConfiguration();
        }
    }
}