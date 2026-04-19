using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _db;
        public CartRepository(AppDbContext db)
        {
            _db = db;
        }
        public async Task AddToCart(CartItem item)
        {
           await _db.CartItems.AddAsync(item);
              await _db.SaveChangesAsync();
        }

        public async Task<CartItem> GetCartItem(int UserId ,int ProductId) { 

        return await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == UserId && c.ProductId == ProductId);
        }

        public Task<CartItem> GetItemCart(int UserId, int ProductId)
        {
         return _db.CartItems.FirstOrDefaultAsync(c => c.UserId == UserId && c.ProductId == ProductId);
        }

        public async Task UpdateItemCart(CartItem item)
        {
            _db.CartItems.Update(item);
            _db.SaveChanges();
        }
    }
}
