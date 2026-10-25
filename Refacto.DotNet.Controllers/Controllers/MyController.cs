using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly ProductService _ps;
        private readonly AppDbContext _ctx;
        private readonly IDateTimeProvider _dateTimeProvider;

        public OrdersController(ProductService ps, AppDbContext ctx, IDateTimeProvider dateTimeProvider)
        {
            _ps = ps;
            _ctx = ctx;
            _dateTimeProvider = dateTimeProvider;
        }

        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(200)]
        public ActionResult<ProcessOrderResponse> ProcessOrder(long orderId)
        {
            Entities.Order? order = _ctx.Orders
                .Include(o => o.Items)
                .SingleOrDefault(o => o.Id == orderId);

            if (order is null)
            {
                return NotFound();
            }

            DateTime currentDate = _dateTimeProvider.Now.Date;
            foreach (Entities.Product p in order.Items ?? new List<Entities.Product>())
            {
                if (p.Type == "NORMAL")
                {
                    if (p.Available > 0)
                    {
                        p.Available -= 1;
                        _ctx.Entry(p).State = EntityState.Modified;
                        _ = _ctx.SaveChanges();

                    }
                    else
                    {
                        int leadTime = p.LeadTime;
                        if (leadTime > 0)
                        {
                            _ps.NotifyDelay(leadTime, p);
                        }
                    }
                }
                else if (p.Type == "SEASONAL")
                {
                    if (p.SeasonStartDate.HasValue
                        && p.SeasonEndDate.HasValue
                        && currentDate > p.SeasonStartDate.Value.Date
                        && currentDate < p.SeasonEndDate.Value.Date
                        && p.Available > 0)
                    {
                        p.Available -= 1;
                        _ = _ctx.SaveChanges();
                    }
                    else
                    {
                        _ps.HandleSeasonalProduct(p);
                    }
                }
                else if (p.Type == "EXPIRABLE")
                {
                    if (p.Available > 0 && p.ExpiryDate.HasValue && p.ExpiryDate.Value.Date > currentDate)
                    {
                        p.Available -= 1;
                        _ = _ctx.SaveChanges();
                    }
                    else
                    {
                        _ps.HandleExpiredProduct(p);
                    }
                }
            }

            return new ProcessOrderResponse(order.Id);
        }
    }
}
