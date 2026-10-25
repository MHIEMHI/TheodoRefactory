namespace Refacto.DotNet.Application.Contracts
{
    public interface IOrderProcessingService
    {
        void ProcessOrder(Domain.Entities.Order order);
    }
}
