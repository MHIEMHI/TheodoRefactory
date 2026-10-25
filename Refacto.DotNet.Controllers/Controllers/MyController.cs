using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderProcessingService _orderProcessingService;

        public OrdersController(IOrderProcessingService orderProcessingService)
        {
            _orderProcessingService = orderProcessingService;
        }

        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(200)]
        public ActionResult<ProcessOrderResponse> ProcessOrder(long orderId)
        {
            long? processedOrderId = _orderProcessingService.ProcessOrder(orderId);
            if (!processedOrderId.HasValue)
            {
                return NotFound();
            }

            return new ProcessOrderResponse(processedOrderId.Value);
        }
    }
}
