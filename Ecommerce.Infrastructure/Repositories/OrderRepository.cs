using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;

namespace Ecommerce.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _db;

        public OrderRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddOrder(Order order)
        {
            await _db.Orders.AddAsync(order);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateOrder(Order order)
        {
            _db.Orders.Update(order);
            await _db.SaveChangesAsync();
        }
    }
}
