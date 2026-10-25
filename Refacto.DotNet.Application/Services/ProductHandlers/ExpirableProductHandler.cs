using Refacto.DotNet.Application.Contracts;
using Refacto.DotNet.Domain.Entities;
using Refacto.DotNet.Domain.Products;

namespace Refacto.DotNet.Application.Services.ProductHandlers
{
    public sealed class ExpirableProductHandler : IProductTypeHandler
    {
        private readonly INotificationService _notifications;

        public ExpirableProductHandler(INotificationService notifications)
        {
            _notifications = notifications;
        }

        public ProductType ProductType => ProductType.Expirable;

        public void Handle(Product product, DateOnly currentDate)
        {
            DateOnly? expiryDate = ToDateOnly(product.ExpiryDate);
            if (product.Available > 0 && expiryDate.HasValue && expiryDate.Value > currentDate)
            {
                product.Available -= 1;
                return;
            }

            if (!product.ExpiryDate.HasValue)
            {
                throw new InvalidOperationException("An expirable product must have an expiry date.");
            }

            _notifications.SendExpirationNotification(product.Name, product.ExpiryDate.Value);
            product.Available = 0;
        }

        private static DateOnly? ToDateOnly(DateTime? value)
        {
            return value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
        }
    }
}
