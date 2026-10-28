

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Domain;
using OrderManagement.Dtos;
using OrderManagement.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db) => _db = db;

    // One projection reused by every query: EF translates it to SQL (no entity loading)
    private static readonly Expression<Func<Category, CategoryDto>> ToDto = c => new CategoryDto
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        ProductCount = c.Products.Count
    };

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct) =>
        await _db.Categories.AsNoTracking().OrderBy(c => c.Name).Select(ToDto).ToListAsync(ct);

    public async Task<CategoryDto> GetAsync(int id, CancellationToken ct)
    {
        var dto = await _db.Categories.AsNoTracking().Where(c => c.Id == id).Select(ToDto).FirstOrDefaultAsync(ct);
        return dto ?? throw new NotFoundException(nameof(Category), id);
    }

    public async Task<CategoryDto> CreateAsync(CategoryWriteDto dto, CancellationToken ct)
    {
        var name = dto.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.Name == name, ct))
            throw new ConflictException($"Category '{name}' already exists.");

        var category = new Category { Name = name, Description = dto.Description?.Trim() };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);

        return await GetAsync(category.Id, ct);
    }

    public async Task<CategoryDto> UpdateAsync(int id, CategoryWriteDto dto, CancellationToken ct)
    {
        var category = await FindEntityAsync(id, ct) ?? throw new NotFoundException(nameof(Category), id);

        var name = dto.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.Name == name && c.Id != id, ct))
            throw new ConflictException($"Category '{name}' already exists.");

        category.Name = name;
        category.Description = dto.Description?.Trim();
        await _db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var category = await FindEntityAsync(id, ct) ?? throw new NotFoundException(nameof(Category), id);

        if (await _db.Products.AnyAsync(p => p.CategoryId == id, ct))
            throw new BusinessRuleException("Cannot delete a category that still has products.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);
    }

    // Nullable return type: callers are forced to handle "not found"
    private Task<Category?> FindEntityAsync(int id, CancellationToken ct) =>
        _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)!;
}