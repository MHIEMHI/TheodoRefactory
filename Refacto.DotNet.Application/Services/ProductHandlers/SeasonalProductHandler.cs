using Refacto.DotNet.Application.Contracts;
using Refacto.DotNet.Domain.Entities;
using Refacto.DotNet.Domain.Products;

namespace Refacto.DotNet.Application.Services.ProductHandlers
{
    public sealed class SeasonalProductHandler : IProductTypeHandler
    {
        private readonly INotificationService _notifications;

        public SeasonalProductHandler(INotificationService notifications)
        {
            _notifications = notifications;
        }

        public ProductType ProductType => ProductType.Seasonal;

        public void Handle(Product product, DateOnly currentDate)
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

        private static DateOnly? ToDateOnly(DateTime? value)
        {
            return value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
        }
    }
}
