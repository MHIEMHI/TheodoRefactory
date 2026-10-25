using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Application.Contracts
{
    public interface IOrderRepository
    {
        Order? GetWithProducts(long orderId);

        void SaveChanges();
    }
}
