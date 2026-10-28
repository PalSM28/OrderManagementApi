using OrderManagement.Dtos;

namespace OrderManagement.Services
{
    public interface IProductService
    {
        Task<PagedResult<ProductDto>> GetPagedAsync(ProductQuery query, CancellationToken ct);
        IAsyncEnumerable<ProductDto> StreamAsync(CancellationToken ct);
        Task<ProductDto> GetAsync(int id, CancellationToken ct);
        Task<string> GetAuditAsync(int id, CancellationToken ct);
        Task<ProductDto> CreateAsync(ProductWriteDto dto, CancellationToken ct);
        Task<ProductDto> UpdateAsync(int id, ProductWriteDto dto, CancellationToken ct);
        Task DeleteAsync(int id, CancellationToken ct);
    }
}