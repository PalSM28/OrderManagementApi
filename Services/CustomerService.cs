using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Domain;
using OrderManagement.Dtos;
using OrderManagement.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _dbContext;

    public CustomerService(AppDbContext dbContext) => _dbContext = dbContext;

    private static readonly Expression<Func<Customer, CustomerDto>> ToDto = c => new CustomerDto
    {
        Id = c.Id,
        FullName = c.FullName,
        Email = c.Email,
        Phone = c.Phone,
        IsPremium = c.IsPremium,
        OrderCount = c.Orders.Count,
        ShippingAddress = c.ShippingAddress == null
            ? null
            : new AddressDto
            {
                Line1 = c.ShippingAddress.Line1,
                City = c.ShippingAddress.City,
                PostalCode = c.ShippingAddress.PostalCode
            }
    };

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct) =>
        await _dbContext.Customers.AsNoTracking().OrderBy(c => c.FullName).Select(ToDto).ToListAsync(ct);

    public async Task<CustomerDto> GetAsync(int id, CancellationToken ct)
    {
        var dto = await _dbContext.Customers.AsNoTracking().Where(c => c.Id == id).Select(ToDto).FirstOrDefaultAsync(ct);
        return dto ?? throw new NotFoundException(nameof(Customer), id);
    }

    public async Task<CustomerDto> CreateAsync(CustomerWriteDto dto, CancellationToken ct)
    {

        var email = dto.Email.Trim().ToLowerInvariant();
        if (_dbContext.Customers.Any(c => c.Email == email))
            throw new ValidationException($"A customer with email '{dto.Email}' already exists.");

        var customer = new Customer
        {
            FullName = dto.FullName,
            Email = dto.Email.Trim().ToLowerInvariant(),
            Phone = dto.Phone,
            IsPremium = dto.IsPremium,
            ShippingAddress = MapAddress(dto.ShippingAddress)
        };
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(ct);
        return await GetAsync(customer.Id, ct);
    }

    private static Address? MapAddress(AddressDto? dto) =>
        dto is null ? null : new Address { Line1 = dto.Line1.Trim(), City = dto.City.Trim(), PostalCode = dto.PostalCode.Trim() };


    public async Task<CustomerDto> UpdateAsync(int id, CustomerWriteDto dto, CancellationToken ct)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
                       ?? throw new NotFoundException(nameof(Customer), id);

        var email = dto.Email.Trim().ToLowerInvariant();
        if (_dbContext.Customers.Any(c => c.Email == email && c.Id != id))
            throw new ValidationException($"A customer with email '{dto.Email}' already exists.");

        customer.FullName = dto.FullName;
        customer.Email = email;
        customer.Phone = dto.Phone;
        customer.IsPremium = dto.IsPremium;
        customer.ShippingAddress = MapAddress(dto.ShippingAddress);
        await _dbContext.SaveChangesAsync(ct);
        return await GetAsync(id, ct);

    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var customer = _dbContext.Customers.FirstOrDefault(c => c.Id == id)
                       ?? throw new NotFoundException(nameof(Customer), id);


        if (await _dbContext.Orders.AnyAsync(o => o.CustomerId == id, ct))
            throw new BusinessRuleException("Cannot delete a customer who has orders.");

        _dbContext.Customers.Remove(customer);
        await _dbContext.SaveChangesAsync(ct);

    }
}
