using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Domain;
using OrderManagement.Dtos;
using OrderManagement.Entity;

namespace OrderManagement.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _dbContext;

        public OrderService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Implement the methods defined in IOrderService here

        private static readonly Expression<Func<Order, OrderDto>> ToDto = o => new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer.FullName,
            OrderDate = o.OrderDate,
            Status = o.Status,
            DiscountRate = o.DiscountRate,
            Subtotal = o.Items.Sum(i => i.Quantity * i.UnitPrice),
            Total = o.Items.Sum(i => i.Quantity * i.UnitPrice) * (1 - o.DiscountRate),
            RowVersion = o.RowVersion,
            Items = o.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.Quantity * i.UnitPrice
            }).ToList()
        };

        public async Task<PagedResult<OrderDto>> GetPagedAsync(OrderQuery query, CancellationToken ct)
        {
            var page = Math.Max(query.Page, 1);
            var size = Math.Clamp(query.PageSize, 1, 100);

            var orders = _dbContext.Orders.AsNoTracking();

            if (query.CustomerId is int customerId)
                orders = orders.Where(o => o.CustomerId == customerId);
            if (query.Status is OrderStatus status)
                orders = orders.Where(o => o.Status == status);

            var total = await orders.CountAsync(ct);
            var items = await orders
                .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(ToDto)
                .ToListAsync(ct);

            return new PagedResult<OrderDto> { Items = items, Page = page, PageSize = size, TotalCount = total };
        }

        public async Task<OrderDto> GetAsync(int id, CancellationToken ct)
        {
            var dto = await _dbContext.Orders.AsNoTracking().Where(o => o.Id == id).Select(ToDto).FirstOrDefaultAsync(ct);
            return dto ?? throw new NotFoundException(nameof(Order), id);
        }

        // Demonstrates Index (^) and Range (..) on an array.
        // In production you would do OrderByDescending().Take(3) in SQL instead of loading everything.
        public async Task<OrderDto[]> GetRecentForCustomerAsync(int customerId, CancellationToken ct)
        {
            if (!await _dbContext.Customers.AnyAsync(c => c.Id == customerId, ct))
                throw new NotFoundException(nameof(Customer), customerId);

            var all = await _dbContext.Orders.AsNoTracking()
                .Where(o => o.CustomerId == customerId)
                .OrderBy(o => o.OrderDate).ThenBy(o => o.Id)
                .Select(ToDto)
                .ToArrayAsync(ct);

            var take = Math.Min(3, all.Length);
            return all[^take..];   // the last "take" elements
        }

        public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken ct)
        {
            var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == dto.CustomerId, ct)
                           ?? throw new NotFoundException(nameof(Customer), dto.CustomerId);

            // merge duplicate lines for the same product; named tuple elements
            var lines = dto.Items
                .GroupBy(i => i.ProductId)
                .Select(g => (ProductId: g.Key, Quantity: g.Sum(i => i.Quantity)))
                .ToList();

            var ids = lines.Select(l => l.ProductId).ToList();
            var products = await _dbContext.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

            var order = new Order
            {
                OrderNumber = NewOrderNumber(),
                CustomerId = customer.Id,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending
            };

            foreach (var (productId, quantity) in lines)
            {
                products.TryGetValue(productId, out var product);

                // C# 8: switch statement with null pattern + property pattern + when guard
                switch (product)
                {
                    case null:
                        throw new NotFoundException(nameof(Product), productId);
                    case { Stock: var stock, Name: var name } when stock < quantity:
                        throw new BusinessRuleException($"Only {stock} unit(s) of '{name}' in stock, but {quantity} requested.");
                }

                product.Stock -= quantity;   // the Product RowVersion makes concurrent stock changes fail with 409
                order.Items.Add(new OrderItem { ProductId = productId, Quantity = quantity, UnitPrice = product.Price });
            }

            var subtotal = order.Items.Sum(i => i.Quantity * i.UnitPrice);
            order.DiscountRate = GetDiscountRate(subtotal, customer.IsPremium);

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync(ct);   // one SaveChanges = one transaction (order, items, stock updates)

            return await GetAsync(order.Id, ct);
        }

        public async Task<OrderDto> UpdateAsync(int id, UpdateOrderStatusDto dto, CancellationToken ct)
        {
            var order = await _dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct)
                        ?? throw new NotFoundException(nameof(Order), id);

            if (!IsValidTransition(order.Status, dto.Status))
                throw new BusinessRuleException($"An order cannot move from {order.Status} to {dto.Status}.");

            _dbContext.Entry(order).Property(o => o.RowVersion).OriginalValue = dto.RowVersion;

            if (dto.Status == OrderStatus.Cancelled)
            {
                // put the stock back
                var ids = order.Items.Select(i => i.ProductId).ToList();
                var products = await _dbContext.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

                foreach (var item in order.Items)
                {
                    if (products.TryGetValue(item.ProductId, out var product))
                        product.Stock += item.Quantity;
                }
            }

            order.Status = dto.Status;
            await _dbContext.SaveChangesAsync(ct);

            return await GetAsync(id, ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct)
        {
            var order = await _dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct)
                        ?? throw new NotFoundException(nameof(Order), id);

            if (order.Status != OrderStatus.Cancelled)
                throw new BusinessRuleException("Only cancelled orders can be deleted.");

            _dbContext.Orders.Remove(order);   // items are removed by cascade
            await _dbContext.SaveChangesAsync(ct);
        }

        // C# 8: switch expression over a tuple (tuple pattern) with "when" guards.
        // Note: relational patterns like "> 1000" arrive in C# 9, so guards are used here.
        private static decimal GetDiscountRate(decimal subtotal, bool isPremium) => (isPremium, subtotal) switch
        {
            (true, var s) when s >= 1000m => 0.15m,
            (false, var s) when s >= 1000m => 0.10m,
            (true, _) => 0.05m,
            _ => 0m
        };

        private static bool IsValidTransition(OrderStatus from, OrderStatus to) => (from, to) switch
        {
            (OrderStatus.Pending, OrderStatus.Paid) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Paid, OrderStatus.Shipped) => true,
            (OrderStatus.Paid, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false
        };

        private static string NewOrderNumber()
        {
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();   // C# 8 range on a string
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
        }


        public async IAsyncEnumerable<OrderDto> StreamAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken ct)
        {
            var query = _dbContext.Orders
                .AsNoTracking()
                .OrderBy(o => o.Id)
                .Select(ToDto);

            await foreach (var order in query
                .AsAsyncEnumerable()
                .WithCancellation(ct))
            {
                yield return order;
            }
        }



        public async Task<string> GetAuditAsync(
            int id,
            CancellationToken ct)
        {
            var order = await _dbContext.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id, ct)
                ?? throw new NotFoundException(nameof(Order), id);

            IAuditable auditable = (IAuditable)order;
            return auditable.Describe();
        }



    }
}