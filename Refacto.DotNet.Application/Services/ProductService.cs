using Refacto.DotNet.Application.Contracts;
using Refacto.DotNet.Domain.Entities;
using Refacto.DotNet.Domain.Products;

namespace Refacto.DotNet.Application.Services
{
    public sealed class ProductService : IProductRulesService
    {
        private readonly INotificationService _notifications;

        public ProductService(INotificationService notifications)
        {
            _notifications = notifications;
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
            }
            else if (product.LeadTime > 0)
            {
                _notifications.SendDelayNotification(product.LeadTime, product.Name);
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
            }
            else if (seasonEndDate.HasValue
                && currentDate.AddDays(product.LeadTime) >= seasonEndDate.Value)
            {
                _notifications.SendOutOfStockNotification(product.Name);
                product.Available = 0;
            }
            else if (seasonStartDate.HasValue && seasonStartDate.Value > currentDate)
            {
                _notifications.SendOutOfStockNotification(product.Name);
            }
            else
            {
                _notifications.SendDelayNotification(product.LeadTime, product.Name);
            }
        }

        private void ProcessExpirableProduct(Product product, DateOnly currentDate)
        {
            DateOnly? expiryDate = ToDateOnly(product.ExpiryDate);
            if (product.Available > 0 && expiryDate.HasValue && expiryDate.Value > currentDate)
            {
                product.Available -= 1;
            }
            else
            {
                if (!product.ExpiryDate.HasValue)
                {
                    throw new InvalidOperationException("An expirable product must have an expiry date.");
                }

                _notifications.SendExpirationNotification(product.Name, product.ExpiryDate.Value);
                product.Available = 0;
            }
        }

        private static DateOnly? ToDateOnly(DateTime? value)
        {
            return value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
        }
    }
}
