using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Pricing;

namespace Vipps.Helpers
{
    public static class VippsExpressCartHelper
    {
        private static Injected<ReferenceConverter> _referenceConverter;
        private static Injected<IContentLoader> _contentLoader;
        private static Injected<IRelationRepository> _relationRepository;
        private static Injected<IPriceService> _priceService;
        private static Injected<IPlacedPriceProcessor> _placedPriceProcessor;

        public static void RemoveAllLineItems(ICart cart)
        {
            var shipment = cart.GetFirstShipment();
            var existingLineItems = cart.GetAllLineItems().ToList();
            foreach (var existingLineItem in existingLineItems)
            {
                shipment.LineItems.Remove(existingLineItem);
            }
        }

        public static void AddToCart(ICart cart, string code, decimal quantity)
        {
            var contentLink = _referenceConverter.Service.GetContentLink(code);
            var entryContent = _contentLoader.Service.Get<EntryContentBase>(contentLink);

            if (entryContent is BundleContent)
            {
                foreach (var relation in _relationRepository.Service.GetChildren<BundleEntry>(contentLink))
                {
                    var entry = _contentLoader.Service.Get<EntryContentBase>(relation.Child);
                    AddToCart(cart, entry.Code, relation.Quantity ?? 1);
                }
                return;
            }

            var lineItem = cart.CreateLineItem(code);
            lineItem.Quantity = quantity;
            AddNewLineItem(cart, lineItem.Code, quantity, entryContent.DisplayName);
        }

        private static void AddNewLineItem(ICart cart, string newCode, decimal quantity, string displayName)
        {
            var newLineItem = cart.CreateLineItem(newCode);
            newLineItem.Quantity = quantity;
            newLineItem.DisplayName = displayName;
            cart.AddLineItem(newLineItem);

            var price = _priceService.Service.GetPrices(cart.MarketId, DateTime.Now, new CatalogKey(newCode),
                    new PriceFilter {Currencies = new[] {cart.Currency}})
                .OrderBy(x => x.UnitPrice.Amount).FirstOrDefault();

            if (price != null)
            {
                newLineItem.PlacedPrice = price.UnitPrice.Amount;
            }
        }

        public static Dictionary<ILineItem, ValidationIssue> ValidateCart(ICart cart)
        {
            var validationIssues = new Dictionary<ILineItem, ValidationIssue>();
            cart.ValidateOrRemoveLineItems((item, issue) => validationIssues.Add(item, issue));
            
            cart.UpdatePlacedPriceOrRemoveLineItems(CustomerContext.Current.GetContactById (cart.CustomerId),
                (item, issue) => validationIssues.Add(item, issue), _placedPriceProcessor.Service);

            cart.UpdateInventoryOrRemoveLineItems((item, issue) => validationIssues.Add(item, issue));

            cart.ApplyDiscounts();

            cart.UpdateInventoryOrRemoveLineItems((item, issue) => {});

            return validationIssues;
        }
    }
}
