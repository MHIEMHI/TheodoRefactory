using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public class ProductService
    {
        private readonly INotificationService _ns;
        private readonly AppDbContext _ctx;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ProductService(INotificationService ns, AppDbContext ctx, IDateTimeProvider dateTimeProvider)
        {
            _ns = ns;
            _ctx = ctx;
            _dateTimeProvider = dateTimeProvider;
        }

        public void NotifyDelay(int leadTime, Product p)
        {
            p.LeadTime = leadTime;
            _ = _ctx.SaveChanges();
            _ns.SendDelayNotification(leadTime, p.Name);
        }

        public void HandleSeasonalProduct(Product p)
        {
            DateTime currentDate = _dateTimeProvider.Now.Date;
            if (p.SeasonEndDate.HasValue && currentDate.AddDays(p.LeadTime) >= p.SeasonEndDate.Value.Date)
            {
                _ns.SendOutOfStockNotification(p.Name);
                p.Available = 0;
                _ = _ctx.SaveChanges();
            }
            else if (p.SeasonStartDate.HasValue && p.SeasonStartDate.Value.Date > currentDate)
            {
                _ns.SendOutOfStockNotification(p.Name);
                _ = _ctx.SaveChanges();
            }
            else
            {
                NotifyDelay(p.LeadTime, p);
            }
        }

        public void HandleExpiredProduct(Product p)
        {
            DateTime currentDate = _dateTimeProvider.Now.Date;
            if (p.Available > 0 && p.ExpiryDate.HasValue && p.ExpiryDate.Value.Date > currentDate)
            {
                p.Available -= 1;
                _ = _ctx.SaveChanges();
            }
            else
            {
                _ns.SendExpirationNotification(p.Name, (DateTime)p.ExpiryDate);
                p.Available = 0;
                _ = _ctx.SaveChanges();
            }
        }
    }
}
