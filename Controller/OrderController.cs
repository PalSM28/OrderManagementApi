using Microsoft.AspNetCore.Mvc;
using OrderManagement.Dtos;
using OrderManagement.Services;

namespace OrderManagement.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrdersController(IOrderService service) => _service = service;

        // GET api/orders?customerId=1&status=Pending&page=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<PagedResult<OrderDto>>> GetPaged([FromQuery] OrderQuery query, CancellationToken ct) =>
            Ok(await _service.GetPagedAsync(query, ct));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken ct) =>
            Ok(await _service.GetAsync(id, ct));

        [HttpGet("customer/{customerId:int}/recent")]
        public async Task<ActionResult<OrderDto[]>> GetRecent(int customerId, CancellationToken ct) =>
            Ok(await _service.GetRecentForCustomerAsync(customerId, ct));

        [HttpPost]
        public async Task<ActionResult<OrderDto>> Create(CreateOrderDto dto, CancellationToken ct)
        {
            var created = await _service.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PATCH api/orders/5/status   body: { "status": "Paid", "rowVersion": "<value from GET>" }
        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<OrderDto>> UpdateStatus(int id, UpdateOrderStatusDto dto, CancellationToken ct) =>
            Ok(await _service.UpdateAsync(id, dto, ct));

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _service.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}