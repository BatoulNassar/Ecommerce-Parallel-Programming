using Ecommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public interface ICartRepository
    {
        Task AddToCart(CartItem item);
        Task<CartItem> GetItemCart(int UserId, int ProductId);
        Task UpdateItemCart (CartItem item);

    }
}
