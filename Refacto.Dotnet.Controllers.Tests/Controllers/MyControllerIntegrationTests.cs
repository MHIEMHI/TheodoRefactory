using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Controllers
{
    [Collection("Sequential")]
    public class MyControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly AppDbContext _context;
        private readonly Mock<INotificationService> _mockNotificationService;

        public MyControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _mockNotificationService = new Mock<INotificationService>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                _ = builder.ConfigureServices(services =>
                {
                    _ = services.AddSingleton(_mockNotificationService.Object);

                    ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        _ = services.Remove(descriptor);
                    }

                    // Add ApplicationDbContext using an in-memory database for testing
                    _ = services.AddDbContext<AppDbContext>(options =>
                    {
                        _ = options.UseInMemoryDatabase($"InMemoryDbForTesting-{GetType()}");
                    });
                    _ = services.AddScoped((_sp) => _mockNotificationService.Object);


                    ServiceProvider sp = services.BuildServiceProvider();
                });
            });

            IServiceScope scope = _factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task ProcessOrderShouldReturn()
        {
            HttpClient client = _factory.CreateClient();

            List<Product> allProducts = CreateProducts();
            HashSet<Product> orderItems = new(allProducts);
            Order order = CreateOrder(orderItems);
            await _context.Products.AddRangeAsync(allProducts);
            _ = await _context.Orders.AddAsync(order);
            _ = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            HttpResponseMessage response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            ProcessOrderResponse? responseBody = await response.Content.ReadFromJsonAsync<ProcessOrderResponse>();
            Assert.NotNull(responseBody);
            Assert.Equal(order.Id, responseBody?.id);

            foreach (Product product in allProducts)
            {
                int expectedAvailability = product.Name switch
                {
                    "USB Cable" or "Butter" or "Watermelon" => product.Available - 1,
                    "Milk" => 0,
                    _ => product.Available
                };

                Product persistedProduct = await _context.Products.AsNoTracking()
                    .SingleAsync(p => p.Id == product.Id);
                Assert.Equal(expectedAvailability, persistedProduct.Available);
            }

            _mockNotificationService.Verify(
                service => service.SendDelayNotification(10, "USB Dongle"),
                Times.Once);
            _mockNotificationService.Verify(
                service => service.SendOutOfStockNotification("Grapes"),
                Times.Once);
            _mockNotificationService.Verify(
                service => service.SendExpirationNotification(
                    "Milk",
                    It.Is<DateTime>(expiryDate =>
                        expiryDate.Date == allProducts.Single(p => p.Name == "Milk").ExpiryDate!.Value.Date)),
                Times.Once);
        }

        [Fact]
        public async Task ProcessOrderForMissingOrderReturnsNotFound()
        {
            HttpClient client = _factory.CreateClient();

            HttpResponseMessage response = await client.PostAsync("/orders/-1/processOrder", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static Order CreateOrder(HashSet<Product> products)
        {
            return new Order { Items = products };
        }

        private static List<Product> CreateProducts()
        {
            return new List<Product>
            {
                new Product { LeadTime = 15, Available = 30, Type = "NORMAL", Name = "USB Cable" },
                new Product { LeadTime = 10, Available = 0, Type = "NORMAL", Name = "USB Dongle" },
                new Product { LeadTime = 15, Available = 30, Type = "EXPIRABLE", Name = "Butter", ExpiryDate = DateTime.Now.AddDays(26) },
                new Product { LeadTime = 90, Available = 6, Type = "EXPIRABLE", Name = "Milk", ExpiryDate = DateTime.Now.AddDays(-2) },
                new Product { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Watermelon", SeasonStartDate = DateTime.Now.AddDays(-2), SeasonEndDate = DateTime.Now.AddDays(58) },
                new Product { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Grapes", SeasonStartDate = DateTime.Now.AddDays(180), SeasonEndDate = DateTime.Now.AddDays(240) }
            };
        }
    }
}
