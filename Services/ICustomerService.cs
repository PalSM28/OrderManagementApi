using OrderManagement.Dtos;
namespace OrderManagement.Services
{
    public interface ICustomerService
    {
        Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct);
        Task<CustomerDto> GetAsync(int id, CancellationToken ct);
        Task<CustomerDto> CreateAsync(CustomerWriteDto dto, CancellationToken ct);
        Task<CustomerDto> UpdateAsync(int id, CustomerWriteDto dto, CancellationToken ct);
        Task DeleteAsync(int id, CancellationToken ct);
    }
}

