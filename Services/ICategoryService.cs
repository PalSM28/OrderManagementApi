using OrderManagement.Dtos;

namespace OrderManagement.Services
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct);
        Task<CategoryDto> GetAsync(int id, CancellationToken ct);
        Task<CategoryDto> CreateAsync(CategoryWriteDto dto, CancellationToken ct);
        Task<CategoryDto> UpdateAsync(int id, CategoryWriteDto dto, CancellationToken ct);
        Task DeleteAsync(int id, CancellationToken ct);
    }
}