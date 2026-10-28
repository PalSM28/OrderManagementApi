
namespace OrderManagement.Domain
{

    public enum OrderStatus { Pending, Paid, Shipped, Delivered, Cancelled }
    public class Customer
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public bool IsPremium { get; set; }

        public Address? ShippingAddress { get; set; } = new Address();

        public DateTime CreatedUtc { get; set; }

        public DateTime? UpdatedUtc { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();


    }

    // Order * --- 1 Customer,  Order 1 --- * OrderItem
    public class Order
    {
        public int Id { get; set; }

        public string OrderName { get; set; } = string.Empty;

        public string OrderNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }

        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal DiscountRate { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public Customer Customer { get; set; } = null!;
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    // Join entity (Order * --- * Product) carrying extra data: quantity and the price at purchase time
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }

    // Owned type: stored in the Customers table (columns ShippingAddress_*)
    public class Address
    {
        public string Line1 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
    }

    // Product * --- 1 Category,  Product 1 --- * OrderItem
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();   // optimistic concurrency token

        public Category Category { get; set; } = null!;   // set by EF; "= null!" is the usual EF idiom
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    // Category 1 --- * Product
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;   // non-nullable: must have a value
        public string? Description { get; set; }            // nullable: optional
        public DateTime CreatedUtc { get; set; }
        public DateTime? UpdatedUtc { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}