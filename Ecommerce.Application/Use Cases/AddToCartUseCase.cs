using Ecommerce.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ecommerce.Domain.Entities;
using System.ComponentModel;

namespace Ecommerce.Application.Use_Cases
{
    public class AddToCartUseCase
    {
        private readonly ICartRepository _cartRepo;
        public AddToCartUseCase(ICartRepository cartRepo)
        {
            _cartRepo = cartRepo;
        }
        public async Task addCartItem(int UserId , int ProductId , int quantity) {

            var ExititingCartItem = await _cartRepo.GetItemCart(UserId, ProductId);
            if (ExititingCartItem != null)
            {
                ExititingCartItem.Quantity += quantity;
                await _cartRepo.UpdateItemCart(ExititingCartItem);

            }
            var CartItem = new CartItem
            {
                ProductId = ProductId,
                UserId = UserId,
                Quantity = quantity,
            };
            await _cartRepo.AddToCart(CartItem);
        }
    }
}

