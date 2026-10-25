namespace Refacto.DotNet.Controllers.Services
{
    public interface IOrderProcessingService
    {
        long? ProcessOrder(long orderId);
    }
}
