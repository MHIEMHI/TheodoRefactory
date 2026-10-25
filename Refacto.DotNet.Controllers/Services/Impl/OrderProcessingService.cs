using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public sealed class OrderProcessingService : IOrderProcessingService
    {
        private readonly AppDbContext _context;
        private readonly IProductRulesService _productRules;
        private readonly IDateTimeProvider _dateTimeProvider;

        public OrderProcessingService(
            AppDbContext context,
            IProductRulesService productRules,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _productRules = productRules;
            _dateTimeProvider = dateTimeProvider;
        }

        public long? ProcessOrder(long orderId)
        {
            Entities.Order? order = _context.Orders
                .Include(candidate => candidate.Items)
                .SingleOrDefault(candidate => candidate.Id == orderId);

            if (order is null)
            {
                return null;
            }

            DateOnly currentDate = DateOnly.FromDateTime(_dateTimeProvider.Now);
            foreach (Entities.Product product in order.Items ?? new List<Entities.Product>())
            {
                _productRules.ProcessProduct(product, currentDate);
            }

            return order.Id;
        }
    }
}
