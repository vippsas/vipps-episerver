using System;
using System.Linq;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Web.Console.Interfaces;
using Newtonsoft.Json;
using Vipps.Extensions;

namespace Vipps.CommerceManager.Apps.Order.Payments.Plugins.Vipps
{
    public partial class ConfigurePayment : System.Web.UI.UserControl, IGatewayControl
    {
        public string ValidationGroup { get; set; }
        private IMarketService _marketService;
        private PaymentMethodDto _paymentMethodDto;
        private IVippsConfigurationLoader _configurationLoader;

        protected override void OnLoad(EventArgs e)
        {
            _marketService = ServiceLocator.Current.GetInstance<IMarketService>();
            _configurationLoader = ServiceLocator.Current.GetInstance<IVippsConfigurationLoader>();
            if (IsPostBack || _paymentMethodDto?.PaymentMethodParameter == null) return;

            var markets = _paymentMethodDto.PaymentMethod.First().GetMarketPaymentMethodsRows();
            if (markets == null || markets.Length == 0) return;

            var market = _marketService.GetMarket(markets.First().MarketId);
            var configuration = GetConfiguration(market.MarketId, market.DefaultLanguage.Name);
            BindConfigurationData(configuration);
            BindLegacyConfiguration();
            BindMarketData(markets);
        }       

        private void BindMarketData(PaymentMethodDto.MarketPaymentMethodsRow[] markets)
        {
            marketDropDownList.DataSource = markets.Select(m => m.MarketId);
            marketDropDownList.DataBind();
        }
        
        
        public void LoadObject(object dto)
        {
            var paymentMethod = dto as PaymentMethodDto;
            if (paymentMethod == null)
            {
                return;
            }
            _paymentMethodDto = paymentMethod;
        }

        public void BindConfigurationData(VippsConfiguration configuration)
        {
            txtClientId.Text = configuration.ClientId;
            txtClientSecret.Text = configuration.ClientSecret;
            txtSubscriptionKey.Text = configuration.SubscriptionKey;
            txtSerialNumber.Text = configuration.MerchantSerialNumber;
            txtSystemName.Text = configuration.SystemName;
            txtApiUrl.Text = configuration.ApiUrl;
            txtSiteBaseUrl.Text = configuration.SiteBaseUrl;
            txtFallbackUrl.Text = configuration.FallbackUrl;
            txtTransactionMessage.Text = configuration.TransactionMessage;
        }
        
        private void BindLegacyConfiguration()
        {
            var configuration = _paymentMethodDto.GetLegacyVippsConfiguration();
            txtLegacyClientId.Text = configuration.ClientId;
            txtLegacyClientSecret.Text = configuration.ClientSecret;
            txtLegacySubscriptionKey.Text = configuration.SubscriptionKey;
            txtLegacySerialNumber.Text = configuration.MerchantSerialNumber;
            txtLegacyApiUrl.Text = configuration.ApiUrl;
            txtLegacySiteBaseUrl.Text = configuration.SiteBaseUrl;
            txtLegacyFallbackUrl.Text = configuration.FallbackUrl;
            txtLegacyTransactionMessage.Text = configuration.TransactionMessage;
        }

        public void SaveChanges(object dto)
        {
            if (!Visible)
            {
                return;
            }

            var paymentMethod = dto as PaymentMethodDto;
            if (paymentMethod == null)
            {
                return;
            }
            
            var currentMarket = marketDropDownList.SelectedValue;

            var configuration = new VippsConfiguration
            {
                ClientId = txtClientId.Text,
                ClientSecret = txtClientSecret.Text,
                SubscriptionKey = txtSubscriptionKey.Text,
                MerchantSerialNumber = txtSerialNumber.Text,
                SystemName = txtSystemName.Text,
                ApiUrl = txtApiUrl.Text,
                SiteBaseUrl = txtSiteBaseUrl.Text,
                FallbackUrl = txtFallbackUrl.Text,
                TransactionMessage = txtTransactionMessage.Text,
                MarketId = marketDropDownList.SelectedValue
            };

            var serialized = JsonConvert.SerializeObject(configuration);
            paymentMethod.SetParameter($"{currentMarket}_{VippsConstants.VippsSerializedMarketOptions}", serialized);
        }
        
        private VippsConfiguration GetConfiguration(MarketId marketId, string languageId)
        {
            try
            {
                return _configurationLoader.GetConfiguration(marketId, languageId);
            }
            catch
            {
                return new VippsConfiguration();
            }
        }

        protected void marketDropDownList_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            var market = _marketService.GetMarket(new MarketId(marketDropDownList.SelectedValue));
            var configuration = GetConfiguration(new MarketId(marketDropDownList.SelectedValue), market.DefaultLanguage.Name);
            BindConfigurationData(configuration);
            ConfigureUpdatePanelContentPanel.Update();
        }
    }
}