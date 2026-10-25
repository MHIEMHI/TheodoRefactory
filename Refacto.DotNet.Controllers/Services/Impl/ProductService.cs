using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public sealed class ProductService : IProductRulesService
    {
        private readonly INotificationService _notifications;
        private readonly AppDbContext _context;

        public ProductService(INotificationService notifications, AppDbContext context)
        {
            _notifications = notifications;
            _context = context;
        }

        public void ProcessProduct(Product product, DateOnly currentDate)
        {
            if (!ProductTypeParser.TryParse(product.Type, out ProductType productType))
            {
                return;
            }

            switch (productType)
            {
                case ProductType.Normal:
                    ProcessNormalProduct(product);
                    break;
                case ProductType.Seasonal:
                    ProcessSeasonalProduct(product, currentDate);
                    break;
                case ProductType.Expirable:
                    ProcessExpirableProduct(product, currentDate);
                    break;
            }
        }

        private void ProcessNormalProduct(Product product)
        {
            if (product.Available > 0)
            {
                product.Available -= 1;
                _ = _context.SaveChanges();
            }
            else if (product.LeadTime > 0)
            {
                NotifyDelay(product.LeadTime, product);
            }
        }

        private void ProcessSeasonalProduct(Product product, DateOnly currentDate)
        {
            DateOnly? seasonStartDate = ToDateOnly(product.SeasonStartDate);
            DateOnly? seasonEndDate = ToDateOnly(product.SeasonEndDate);

            if (seasonStartDate.HasValue
                && seasonEndDate.HasValue
                && currentDate > seasonStartDate.Value
                && currentDate < seasonEndDate.Value
                && product.Available > 0)
            {
                product.Available -= 1;
                _ = _context.SaveChanges();
            }
            else if (seasonEndDate.HasValue
                && currentDate.AddDays(product.LeadTime) >= seasonEndDate.Value)
            {
                _notifications.SendOutOfStockNotification(product.Name);
                product.Available = 0;
                _ = _context.SaveChanges();
            }
            else if (seasonStartDate.HasValue && seasonStartDate.Value > currentDate)
            {
                _notifications.SendOutOfStockNotification(product.Name);
                _ = _context.SaveChanges();
            }
            else
            {
                NotifyDelay(product.LeadTime, product);
            }
        }

        private void ProcessExpirableProduct(Product product, DateOnly currentDate)
        {
            DateOnly? expiryDate = ToDateOnly(product.ExpiryDate);
            if (product.Available > 0 && expiryDate.HasValue && expiryDate.Value > currentDate)
            {
                product.Available -= 1;
                _ = _context.SaveChanges();
            }
            else
            {
                _notifications.SendExpirationNotification(product.Name, product.ExpiryDate!.Value);
                product.Available = 0;
                _ = _context.SaveChanges();
            }
        }

        private void NotifyDelay(int leadTime, Product product)
        {
            product.LeadTime = leadTime;
            _ = _context.SaveChanges();
            _notifications.SendDelayNotification(leadTime, product.Name);
        }

        private static DateOnly? ToDateOnly(DateTime? value)
        {
            return value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
        }
    }
}
