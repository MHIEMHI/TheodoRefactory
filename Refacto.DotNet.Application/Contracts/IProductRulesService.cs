using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Application.Contracts
{
    public interface IProductRulesService
    {
        void ProcessProduct(Product product, DateOnly currentDate);
    }
}
