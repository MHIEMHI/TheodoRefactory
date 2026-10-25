using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services
{
    public interface IProductRulesService
    {
        void ProcessProduct(Product product, DateOnly currentDate);
    }
}
