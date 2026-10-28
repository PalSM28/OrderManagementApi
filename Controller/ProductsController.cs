using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Dtos;
using OrderManagement.Services;

namespace OrderManagement.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service) => _service = service;

        // GET api/products?page=1&pageSize=10&categoryId=1&search=mouse
        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductDto>>> GetPaged([FromQuery] ProductQuery query, CancellationToken ct) =>
            Ok(await _service.GetPagedAsync(query, ct));

        // GET api/products/stream  (IAsyncEnumerable - C# 8 async stream)
        [HttpGet("stream")]
        public async IAsyncEnumerable<ProductDto> Stream([EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var product in _service.StreamAsync(ct).WithCancellation(ct))
                yield return product;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct) =>
            Ok(await _service.GetAsync(id, ct));

        [HttpGet("{id:int}/audit")]
        public async Task<ActionResult<string>> GetAudit(int id, CancellationToken ct) =>
            Ok(await _service.GetAuditAsync(id, ct));

        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create(ProductWriteDto dto, CancellationToken ct)
        {
            var created = await _service.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ProductDto>> Update(int id, ProductWriteDto dto, CancellationToken ct) =>
            Ok(await _service.UpdateAsync(id, dto, ct));

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _service.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}