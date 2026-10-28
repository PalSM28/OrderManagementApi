
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain;

namespace OrderManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Category>(e =>
            {
                e.Property(x => x.Name).IsRequired().HasMaxLength(100);
                e.Property(x => x.Description).HasMaxLength(500);
                e.HasIndex(x => x.Name).IsUnique();
            });

            b.Entity<Product>(e =>
            {
                e.Property(x => x.Name).IsRequired().HasMaxLength(150);
                e.Property(x => x.Sku).IsRequired().HasMaxLength(40);
                e.Property(x => x.Price).HasColumnType("decimal(18,2)");
                e.Property(x => x.RowVersion).IsRowVersion();
                e.HasIndex(x => x.Sku).IsUnique();

                // Category 1 - * Product (cannot delete a category that still has products)
                e.HasOne(x => x.Category)
                 .WithMany(c => c.Products)
                 .HasForeignKey(x => x.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<Customer>(e =>
            {
                e.Property(x => x.FullName).IsRequired().HasMaxLength(150);
                e.Property(x => x.Email).IsRequired().HasMaxLength(200);
                e.Property(x => x.Phone).HasMaxLength(30);
                e.HasIndex(x => x.Email).IsUnique();
                e.OwnsOne(x => x.ShippingAddress, address =>
                {
                    address.Property(a => a.Line1)
                    .IsRequired()
                    .HasMaxLength(100);
                    address.Property(a => a.City)
                    .IsRequired()
                    .HasMaxLength(100);
                    address.Property(a => a.PostalCode)
                    .IsRequired()
                    .HasMaxLength(20);
                });      // 1:1 owned type, same table
            });

            b.Entity<Order>(e =>
            {
                e.Property(x => x.OrderName).IsRequired().HasMaxLength(30);
                e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.DiscountRate).HasColumnType("decimal(5,4)");
                e.Property(x => x.RowVersion).IsRowVersion();
                e.HasIndex(x => x.OrderName).IsUnique();

                // Customer 1 - * Order 
            });

            base.OnModelCreating(b);
        }
    }
}
