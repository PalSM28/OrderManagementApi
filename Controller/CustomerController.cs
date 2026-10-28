using Microsoft.AspNetCore.Mvc;
using OrderManagement.Dtos;
using OrderManagement.Services;

namespace OrderManagement.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAllAsync(CancellationToken ct)
        {
            var customers = await _customerService.GetAllAsync(ct);
            return Ok(customers);
        }

        [HttpGet("{id:int}", Name = "GetCustomerById")]
        public async Task<ActionResult<CustomerDto>> GetCustomerByIdAsync(int id, CancellationToken ct)
        {
            var customer = await _customerService.GetAsync(id, ct);
            if (customer == null)
            {
                return NotFound();
            }
            return Ok(customer);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerDto>> CreateCustomer(CustomerWriteDto dto, CancellationToken ct)
        {
            var createdCustomer = await _customerService.CreateAsync(dto, ct);
            return CreatedAtRoute("GetCustomerById", new { id = createdCustomer.Id }, createdCustomer);
            //return CreatedAtAction(nameof(GetCustomerByIdAsync), new { id = createdCustomer.Id }, createdCustomer);

            //return Ok(createdCustomer);

            //return Created("api/customer/" + createdCustomer.Id, createdCustomer);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CustomerDto>> UpdateCustomer(int id, CustomerWriteDto dto, CancellationToken ct)
        {
            var updatedCustomer = await _customerService.UpdateAsync(id, dto, ct);
            return Ok(updatedCustomer);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
        {
            await _customerService.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}