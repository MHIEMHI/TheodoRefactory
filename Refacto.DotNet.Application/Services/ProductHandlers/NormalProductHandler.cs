using Refacto.DotNet.Application.Contracts;
using Refacto.DotNet.Domain.Entities;
using Refacto.DotNet.Domain.Products;

namespace Refacto.DotNet.Application.Services.ProductHandlers
{
    public sealed class NormalProductHandler : IProductTypeHandler
    {
        private readonly INotificationService _notifications;

        public NormalProductHandler(INotificationService notifications)
        {
            _notifications = notifications;
        }

        public ProductType ProductType => ProductType.Normal;

        public void Handle(Product product, DateOnly currentDate)
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
    }
}
