namespace Refacto.DotNet.Application.Contracts
{
    public interface IOrderProcessingService
    {
        long? ProcessOrder(long orderId);
    }
}
