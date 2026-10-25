using Microsoft.Extensions.DependencyInjection;
using Refacto.DotNet.Application.Contracts;
using Refacto.DotNet.Application.Services.ProductHandlers;

namespace Refacto.DotNet.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            _ = services.AddScoped<IOrderProcessingService, Services.OrderProcessingService>();
            _ = services.AddScoped<IProductRulesService, Services.ProductService>();
            _ = services.AddScoped<IProductTypeHandler, NormalProductHandler>();
            _ = services.AddScoped<IProductTypeHandler, SeasonalProductHandler>();
            _ = services.AddScoped<IProductTypeHandler, ExpirableProductHandler>();

            return services;
        }
    }
}
