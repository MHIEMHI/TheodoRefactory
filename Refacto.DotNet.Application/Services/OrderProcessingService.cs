using Refacto.DotNet.Application.Contracts;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Application.Services
{
    public sealed class OrderProcessingService : IOrderProcessingService
    {
        private readonly IOrderRepository _orders;
        private readonly IProductRulesService _productRules;
        private readonly IDateTimeProvider _dateTimeProvider;

        public OrderProcessingService(
            IOrderRepository orders,
            IProductRulesService productRules,
            IDateTimeProvider dateTimeProvider)
        {
            _orders = orders;
            _productRules = productRules;
            _dateTimeProvider = dateTimeProvider;
        }

        public long? ProcessOrder(long orderId)
        {
            Order? order = _orders.GetWithProducts(orderId);
            if (order is null)
            {
                return null;
            }

            DateOnly currentDate = DateOnly.FromDateTime(_dateTimeProvider.Now);
            foreach (Product product in order.Items ?? new List<Product>())
            {
                _productRules.ProcessProduct(product, currentDate);
            }

            _orders.SaveChanges();
            return order.Id;
        }
    }
}
