using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Application.Contracts;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Infrastructure.Persistence
{
    public sealed class EfOrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public EfOrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public Order? GetWithProducts(long orderId)
        {
            return _context.Orders
                .Include(order => order.Items)
                .SingleOrDefault(order => order.Id == orderId);
        }

        public void SaveChanges()
        {
            _ = _context.SaveChanges();
        }
    }
}
