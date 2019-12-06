using Mediachase.Commerce.Orders.Dto;
using Mediachase.Web.Console.Interfaces;
using Vipps.Extensions;

namespace Vipps.CommerceManager.Apps.Order.Payments.Plugins.Vipps
{
    public partial class ConfigurePayment : System.Web.UI.UserControl, IGatewayControl
    {
        public string ValidationGroup { get; set; }

        public void LoadObject(object dto)
        {
            var paymentMethod = dto as PaymentMethodDto;

            if (paymentMethod == null)
            {
                return;
            }

            var configuration = paymentMethod.GetVippsConfiguration();
            txtClientId.Text = configuration.ClientId;
            txtClientSecret.Text = configuration.ClientSecret;
            txtSubscriptionKey.Text = configuration.SubscriptionKey;
            txtSerialNumber.Text = configuration.MerchantSerialNumber;
            txtApiUrl.Text = configuration.ApiUrl;
            txtSiteBaseUrl.Text = configuration.SiteBaseUrl;
            txtFallbackUrl.Text = configuration.FallbackUrl;
            txtTransactionMessage.Text = configuration.TransactionMessage;
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

            paymentMethod.SetParameter(VippsConstants.ClientId, txtClientId.Text);
            paymentMethod.SetParameter(VippsConstants.ClientSecret, txtClientSecret.Text);
            paymentMethod.SetParameter(VippsConstants.SubscriptionKey, txtSubscriptionKey.Text);
            paymentMethod.SetParameter(VippsConstants.MerchantSerialNumber, txtSerialNumber.Text);
            paymentMethod.SetParameter(VippsConstants.ApiUrl, txtApiUrl.Text);
            paymentMethod.SetParameter(VippsConstants.SiteBaseUrl, txtSiteBaseUrl.Text);
            paymentMethod.SetParameter(VippsConstants.FallbackUrl, txtFallbackUrl.Text);
            paymentMethod.SetParameter(VippsConstants.TransactionMessage, txtTransactionMessage.Text);
        }
    }
}