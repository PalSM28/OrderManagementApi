using OrderManagement.Dtos;

namespace OrderManagement.Services
{
    public interface IOrderService
    {
        Task<PagedResult<OrderDto>> GetPagedAsync(OrderQuery query, CancellationToken ct);
        IAsyncEnumerable<OrderDto> StreamAsync(CancellationToken ct);
        Task<OrderDto> GetAsync(int id, CancellationToken ct);
        Task<string> GetAuditAsync(int id, CancellationToken ct);
        Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken ct);
        Task<OrderDto> UpdateAsync(int id, UpdateOrderStatusDto dto, CancellationToken ct);
        Task DeleteAsync(int id, CancellationToken ct);

        Task<OrderDto[]> GetRecentForCustomerAsync(int customerId, CancellationToken ct);
    }

}