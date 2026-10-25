using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Refacto.DotNet.Controllers.Controllers;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.Dotnet.Controllers.Tests.Services
{
    public class ProductServiceCharacterizationTests
    {

        private AppDbContext _context = null!;
        private Mock<INotificationService> _notifications = null!;
        private ProductService _service = null!;

        public ProductServiceCharacterizationTests()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _notifications = new Mock<INotificationService>();
            _service = new ProductService(_notifications.Object, _context);
        }


        [Fact]
        public void NotifyDelaySavesTheLeadTimeAndSendsDelayNotification()
        {
            Product product = AddProduct(new Product { LeadTime = 15, Available = 0, Name = "RJ45 Cable" });

            _service.NotifyDelay(15, product);

            Assert.Equal(15, ReadProduct(product.Id).LeadTime);
            _notifications.Verify(service => service.SendDelayNotification(15, "RJ45 Cable"), Times.Once);
        }

        [Fact]
        public void SeasonalProductWhoseLeadTimeExceedsSeasonEndIsDepletedAndReportedOutOfStock()
        {
            Product product = AddProduct(new Product
            {
                LeadTime = 20,
                Available = 8,
                Name = "Watermelon",
                SeasonStartDate = DateTime.Now.AddDays(-1),
                SeasonEndDate = DateTime.Now.AddDays(10)
            });

            _service.HandleSeasonalProduct(product);

            Assert.Equal(0, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendOutOfStockNotification("Watermelon"), Times.Once);
            _notifications.Verify(service => service.SendDelayNotification(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void SeasonalProductBeforeItsStartIsReportedOutOfStockWithoutChangingAvailability()
        {
            Product product = AddProduct(new Product
            {
                LeadTime = 2,
                Available = 8,
                Name = "Grapes",
                SeasonStartDate = DateTime.Now.AddDays(30),
                SeasonEndDate = DateTime.Now.AddDays(60)
            });

            _service.HandleSeasonalProduct(product);

            Assert.Equal(8, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendOutOfStockNotification("Grapes"), Times.Once);
            _notifications.Verify(service => service.SendDelayNotification(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void SeasonalProductWithReachableSeasonUsesDelayNotification()
        {
            Product product = AddProduct(new Product
            {
                LeadTime = 2,
                Available = 8,
                Name = "Peaches",
                SeasonStartDate = DateTime.Now.AddDays(-1),
                SeasonEndDate = DateTime.Now.AddDays(10)
            });

            _service.HandleSeasonalProduct(product);

            Assert.Equal(8, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendDelayNotification(2, "Peaches"), Times.Once);
            _notifications.Verify(service => service.SendOutOfStockNotification(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void SeasonalProductWhoseLeadTimeReachesSeasonEndIsReportedOutOfStock()
        {
            Product product = AddProduct(new Product
            {
                LeadTime = 1,
                Available = 8,
                Name = "Peaches",
                SeasonStartDate = DateTime.Today.AddDays(-1),
                SeasonEndDate = DateTime.Today.AddDays(1)
            });

            _service.HandleSeasonalProduct(product);

            Assert.Equal(0, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendOutOfStockNotification("Peaches"), Times.Once);
            _notifications.Verify(service => service.SendDelayNotification(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void SeasonalProductWithNonPositiveLeadTimeStillUsesDelayNotification(int leadTime)
        {
            Product product = AddProduct(new Product
            {
                LeadTime = leadTime,
                Available = 8,
                Name = "Peaches",
                SeasonStartDate = DateTime.Now.AddDays(-1),
                SeasonEndDate = DateTime.Now.AddDays(10)
            });

            _service.HandleSeasonalProduct(product);

            _notifications.Verify(service => service.SendDelayNotification(leadTime, "Peaches"), Times.Once);
        }

        [Fact]
        public void UnexpiredProductInStockIsDecrementedWithoutNotification()
        {
            Product product = AddProduct(new Product
            {
                Available = 3,
                Name = "Butter",
                ExpiryDate = DateTime.Now.AddDays(2)
            });

            _service.HandleExpiredProduct(product);

            Assert.Equal(2, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendExpirationNotification(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public void ProductExpiringTodayIsZeroedAndReportedExpired()
        {
            Product product = AddProduct(new Product
            {
                Available = 3,
                Name = "Milk",
                ExpiryDate = DateTime.Today
            });

            _service.HandleExpiredProduct(product);

            Assert.Equal(0, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendExpirationNotification("Milk", DateTime.Today), Times.Once);
        }

        [Fact]
        public void OutOfStockProductIsReportedExpiredEvenWhenItsExpiryDateIsInTheFuture()
        {
            DateTime expiryDate = DateTime.Now.AddDays(2);
            Product product = AddProduct(new Product
            {
                Available = 0,
                Name = "Cheese",
                ExpiryDate = expiryDate
            });

            _service.HandleExpiredProduct(product);

            Assert.Equal(0, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendExpirationNotification("Cheese", expiryDate), Times.Once);
        }

        [Fact]
        public void OutOfStockProductWithoutExpiryDateThrowsBeforeSendingNotification()
        {
            Product product = AddProduct(new Product { Available = 0, Name = "Unknown expiry" });

            Assert.Throws<InvalidOperationException>(() => _service.HandleExpiredProduct(product));

            _notifications.Verify(service => service.SendExpirationNotification(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public void InStockProductWithoutExpiryDateIsZeroedAndThrowsBeforeSendingNotification()
        {
            Product product = AddProduct(new Product { Available = 2, Name = "Unknown expiry" });

            Assert.Throws<InvalidOperationException>(() => _service.HandleExpiredProduct(product));

            Assert.Equal(2, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendExpirationNotification(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public void ProcessOrderDecrementsAvailableProductAndReturnsOrderId()
        {
            Product product = new() { Available = 2, Type = "NORMAL", Name = "USB Cable" };
            Order order = AddOrder(product);

            ActionResult<Refacto.DotNet.Controllers.Dtos.Product.ProcessOrderResponse> result =
                CreateController().ProcessOrder(order.Id);

            Assert.Equal(order.Id, result.Value!.id);
            Assert.Equal(1, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendDelayNotification(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ProcessOrderNotifiesForUnavailableNormalProductOnlyWhenLeadTimeIsPositive()
        {
            Product product = new() { Available = 0, LeadTime = 12, Type = "NORMAL", Name = "USB Dongle" };
            Order order = AddOrder(product);

            _ = CreateController().ProcessOrder(order.Id);

            _notifications.Verify(service => service.SendDelayNotification(12, "USB Dongle"), Times.Once);
            Assert.Equal(0, ReadProduct(product.Id).Available);
        }

        [Fact]
        public void ProcessOrderIgnoresUnavailableNormalProductWithoutPositiveLeadTime()
        {
            Product product = new() { Available = 0, LeadTime = 0, Type = "NORMAL", Name = "USB Dongle" };
            Order order = AddOrder(product);

            _ = CreateController().ProcessOrder(order.Id);

            _notifications.Verify(service => service.SendDelayNotification(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ProcessOrderIgnoresUnavailableNormalProductWithNegativeLeadTime()
        {
            Product product = new() { Available = 0, LeadTime = -1, Type = "NORMAL", Name = "USB Dongle" };
            Order order = AddOrder(product);

            _ = CreateController().ProcessOrder(order.Id);

            _notifications.Verify(service => service.SendDelayNotification(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ProcessOrderOnSeasonStartDateFallsBackToZeroDayDelay()
        {
            Product product = new()
            {
                Available = 2,
                LeadTime = 0,
                Type = "SEASONAL",
                Name = "Watermelon",
                SeasonStartDate = DateTime.Today,
                SeasonEndDate = DateTime.Today.AddDays(10)
            };
            Order order = AddOrder(product);

            _ = CreateController().ProcessOrder(order.Id);

            Assert.Equal(2, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendDelayNotification(0, "Watermelon"), Times.Once);
        }

        [Fact]
        public void ProcessOrderOnSeasonEndDateReportsOutOfStockAndSetsAvailabilityToZero()
        {
            Product product = new()
            {
                Available = 2,
                LeadTime = 0,
                Type = "SEASONAL",
                Name = "Watermelon",
                SeasonStartDate = DateTime.Today.AddDays(-10),
                SeasonEndDate = DateTime.Today
            };
            Order order = AddOrder(product);

            _ = CreateController().ProcessOrder(order.Id);

            Assert.Equal(0, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendOutOfStockNotification("Watermelon"), Times.Once);
        }

        [Fact]
        public void ProcessOrderOnExpiryDateReportsExpiredAndSetsAvailabilityToZero()
        {
            Product product = new()
            {
                Available = 2,
                Type = "EXPIRABLE",
                Name = "Milk",
                ExpiryDate = DateTime.Today
            };
            Order order = AddOrder(product);

            _ = CreateController().ProcessOrder(order.Id);

            Assert.Equal(0, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendExpirationNotification("Milk", DateTime.Today), Times.Once);
        }

        [Fact]
        public void ProcessOrderAppliesCurrentSeasonAndExpiryRules()
        {
            Product inSeason = new()
            {
                Available = 2,
                Type = "SEASONAL",
                Name = "Watermelon",
                SeasonStartDate = DateTime.Today.AddDays(-1),
                SeasonEndDate = DateTime.Today.AddDays(1)
            };
            Product futureSeason = new()
            {
                Available = 2,
                LeadTime = 90,
                Type = "SEASONAL",
                Name = "Grapes",
                SeasonStartDate = DateTime.Now.AddDays(30),
                SeasonEndDate = DateTime.Now.AddDays(60)
            };
            Product unexpired = new()
            {
                Available = 2,
                Type = "EXPIRABLE",
                Name = "Butter",
                ExpiryDate = DateTime.Now.AddDays(2)
            };
            Product expired = new()
            {
                Available = 2,
                Type = "EXPIRABLE",
                Name = "Milk",
                ExpiryDate = DateTime.Today.AddDays(-1)
            };
            Order order = AddOrder(inSeason, futureSeason, unexpired, expired);

            ActionResult<Refacto.DotNet.Controllers.Dtos.Product.ProcessOrderResponse> result =
                CreateController().ProcessOrder(order.Id);

            Assert.Equal(order.Id, result.Value!.id);
            Assert.Equal(1, ReadProduct(inSeason.Id).Available);
            Assert.Equal(0, ReadProduct(futureSeason.Id).Available);
            Assert.Equal(1, ReadProduct(unexpired.Id).Available);
            Assert.Equal(0, ReadProduct(expired.Id).Available);
            _notifications.Verify(service => service.SendOutOfStockNotification("Grapes"), Times.Once);
            _notifications.Verify(service => service.SendExpirationNotification("Milk", expired.ExpiryDate!.Value), Times.Once);
        }

        [Fact]
        public void ProcessOrderWithUnknownProductTypeLeavesAvailabilityUnchanged()
        {
            Product product = new() { Available = 5, Type = "OTHER", Name = "Unclassified" };
            Order order = AddOrder(product);

            _ = CreateController().ProcessOrder(order.Id);

            Assert.Equal(5, ReadProduct(product.Id).Available);
            _notifications.Verify(service => service.SendDelayNotification(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _notifications.Verify(service => service.SendOutOfStockNotification(It.IsAny<string>()), Times.Never);
            _notifications.Verify(service => service.SendExpirationNotification(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public void ProcessOrderForMissingOrderReturnsNotFound()
        {
            ActionResult<Refacto.DotNet.Controllers.Dtos.Product.ProcessOrderResponse> result =
                CreateController().ProcessOrder(-1);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        private Product AddProduct(Product product)
        {
            _context.Products.Add(product);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return _context.Products.Single(p => p.Name == product.Name);
        }

        private Product ReadProduct(long id)
        {
            _context.ChangeTracker.Clear();
            return _context.Products.Find(id)!;
        }

        private Order AddOrder(params Product[] products)
        {
            Order order = new() { Items = products.ToList() };
            _context.Orders.Add(order);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return order;
        }

        private OrdersController CreateController()
        {
            return new OrdersController(_service, _context);
        }
    }
}
