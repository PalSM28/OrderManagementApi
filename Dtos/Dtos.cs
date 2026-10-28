using System.ComponentModel.DataAnnotations;
using OrderManagement.Domain;

namespace OrderManagement.Dtos
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    // ---------- Category ----------
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCount { get; set; }
    }

    public class CategoryWriteDto
    {
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        [StringLength(500)] public string? Description { get; set; }
    }

    // ---------- Product ----------
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class ProductWriteDto
    {
        [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
        [Required, StringLength(40)] public string Sku { get; set; } = string.Empty;
        [Range(0.01, 1_000_000)] public decimal Price { get; set; }
        [Range(0, int.MaxValue)] public int Stock { get; set; }
        public int CategoryId { get; set; }
        public byte[]? RowVersion { get; set; }   // send back the value you received to get concurrency checking on PUT
    }

    public class ProductQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? CategoryId { get; set; }
        public string? Search { get; set; }
    }

    // ---------- Customer ----------
    public class AddressDto
    {
        [Required, StringLength(200)] public string Line1 { get; set; } = string.Empty;
        [Required, StringLength(100)] public string City { get; set; } = string.Empty;
        [Required, StringLength(20)] public string PostalCode { get; set; } = string.Empty;
    }

    public class CustomerDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsPremium { get; set; }
        public AddressDto? ShippingAddress { get; set; }
        public int OrderCount { get; set; }
    }

    public class CustomerWriteDto
    {
        [Required, StringLength(150)] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = string.Empty;
        [StringLength(30)] public string? Phone { get; set; }
        public bool IsPremium { get; set; }
        public AddressDto? ShippingAddress { get; set; }
    }

    // ---------- Order ----------
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal Total { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }

    public class OrderItemWriteDto
    {
        public int ProductId { get; set; }
        [Range(1, 1000)] public int Quantity { get; set; }
    }

    public class CreateOrderDto
    {
        public int CustomerId { get; set; }
        [Required, MinLength(1)] public List<OrderItemWriteDto> Items { get; set; } = new List<OrderItemWriteDto>();
    }

    public class UpdateOrderStatusDto
    {
        public OrderStatus Status { get; set; }
        [Required] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class OrderQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? CustomerId { get; set; }
        public OrderStatus? Status { get; set; }
    }

}