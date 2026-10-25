using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Application.Contracts;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderRepository _orders;

        public OrdersController(
            IOrderProcessingService orderProcessingService,
            IOrderRepository orders)
        {
            _orderProcessingService = orderProcessingService;
            _orders = orders;
        }

        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(200)]
        public ActionResult<ProcessOrderResponse> ProcessOrder(long orderId)
        {
            Order? order = _orders.GetWithProducts(orderId);
            if (order is null)
            {
                return NotFound();
            }

            _orderProcessingService.ProcessOrder(order);
            return new ProcessOrderResponse(order.Id);
        }
    }
}
