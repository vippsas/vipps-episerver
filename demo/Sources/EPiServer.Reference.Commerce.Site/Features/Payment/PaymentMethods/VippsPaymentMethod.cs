using System.ComponentModel;
using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.Reference.Commerce.Site.Features.Cart.Services;
using EPiServer.Reference.Commerce.Site.Features.Market.Services;
using EPiServer.Reference.Commerce.Site.Features.Payment.Services;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders;
using Vipps;

namespace EPiServer.Reference.Commerce.Site.Features.Payment.PaymentMethods
{
    [ServiceConfiguration(typeof(IPaymentMethod), Lifecycle = ServiceInstanceScope.Hybrid)]
    public class VippsPaymentMethod : PaymentMethodBase, IDataErrorInfo
    {
        private readonly IOrderGroupFactory _orderGroupFactory;

        public override string SystemKeyword => VippsConstants.VippsSystemKeyword;

        public VippsPaymentMethod()
            : this(
                LocalizationService.Current,
                ServiceLocator.Current.GetInstance<IOrderGroupFactory>(),
                ServiceLocator.Current.GetInstance<LanguageService>(),
                ServiceLocator.Current.GetInstance<IPaymentManagerFacade>())
        {
        }

        public VippsPaymentMethod(
                LocalizationService localizationService,
                IOrderGroupFactory orderGroupFactory,
                LanguageService languageService,
                IPaymentManagerFacade paymentManager)
            : base(localizationService, orderGroupFactory, languageService, paymentManager)
        {
            _orderGroupFactory = orderGroupFactory;
        }


        public override IPayment CreatePayment(decimal amount, IOrderGroup orderGroup)
        {
            var payment = orderGroup.CreatePayment(_orderGroupFactory);
            payment.PaymentType = PaymentType.Other;
            payment.PaymentMethodId = PaymentMethodId;
            payment.PaymentMethodName = VippsConstants.VippsSystemKeyword;
            payment.Amount = amount;
            payment.Status = PaymentStatus.Pending.ToString();
            payment.TransactionType = TransactionType.Authorization.ToString();
            return payment;
        }

        public void PostProcess(IPayment payment)
        {
            payment.Status = PaymentStatus.Processed.ToString();
        }

        public override bool ValidateData()
        {
            return true;
        }

        public string this[string columnName] => string.Empty;

        public string Error { get; }

    }
}