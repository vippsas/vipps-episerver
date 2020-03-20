using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using Mediachase.Commerce;
using Moq;
using Vipps.Models;
using Vipps.Services;
using Vipps.Test.Fakes;
using Xunit;

namespace Vipps.Test.Tests
{
    public class CreatePurchaseOrderTests
    {
        [Fact]
        public void CreateOrderOnMultipleThreads_OnlyTwoShouldBeCreated()
        {
            var cart1 = new FakeCart(new Mock<IMarket>().Object, Currency.NOK);
            var cart2 = new FakeCart(new Mock<IMarket>().Object, Currency.NOK);

            var purchaseOrder1 = new FakePurchaseOrder(new Mock<IMarket>().Object, Currency.NOK);
            var purchaseOrder2 = new FakePurchaseOrder(new Mock<IMarket>().Object, Currency.NOK);

            var orderReference1 = new OrderReference(1, "", Guid.Empty, null);
            var orderReference2 = new OrderReference(2, "", Guid.Empty, null);

            _orderRepositoryMock.Setup(x => x.SaveAsPurchaseOrder(cart1))
                .Returns(() => orderReference1);

            _orderRepositoryMock.Setup(x => x.SaveAsPurchaseOrder(cart2))
                .Returns(() => orderReference2);

            _vippsServiceMock.SetupSequence(x => x.GetPurchaseOrderByOrderId("1"))
                .Returns(null)
                .Returns(purchaseOrder1)
                .Returns(purchaseOrder1)
                .Returns(purchaseOrder1)
                .Returns(purchaseOrder1)
                .Returns(purchaseOrder1)
                .Returns(purchaseOrder1);

            _vippsServiceMock.SetupSequence(x=>x.GetPurchaseOrderByOrderId("2"))
                .Returns(null)
                .Returns(purchaseOrder2);

            var responseList = new List<LoadOrCreatePurchaseOrderResponse>();

            Parallel.Invoke(async () => responseList.Add(await _subject.LoadOrCreatePurchaseOrder(cart1, "1")),
                async () => responseList.Add(await _subject.LoadOrCreatePurchaseOrder(cart1, "1")),
                async () => responseList.Add(await _subject.LoadOrCreatePurchaseOrder(cart1, "1")),
                async () => responseList.Add(await _subject.LoadOrCreatePurchaseOrder(cart1, "1")),
                async () => responseList.Add(await _subject.LoadOrCreatePurchaseOrder(cart2, "2")),
                async () => responseList.Add(await _subject.LoadOrCreatePurchaseOrder(cart1, "1")),
                async () => responseList.Add(await _subject.LoadOrCreatePurchaseOrder(cart2, "2")),
                async () => responseList.Add(await  _subject.LoadOrCreatePurchaseOrder(cart1, "1")),
                async () => responseList.Add(await _subject.LoadOrCreatePurchaseOrder(cart1, "1"))
            );

            var createdOrders = responseList.Where(x => x.PurchaseOrderCreated);

            Assert.Equal(2, createdOrders.Count());
        }

        private readonly Mock<IOrderRepository> _orderRepositoryMock;
        private readonly Mock<IVippsService> _vippsServiceMock;
        private readonly IVippsOrderCreator _subject;

        public CreatePurchaseOrderTests()
        {
            _orderRepositoryMock = new Mock<IOrderRepository>();
            _vippsServiceMock = new Mock<IVippsService>();
            _subject = new DefaultVippsOrderCreator(_orderRepositoryMock.Object, _vippsServiceMock.Object);
        }
    }
}
