using Ecommerce.Application.Use_Cases;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce_Parallel_Programming.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartItemController : ControllerBase
    {
        private readonly AddToCartUseCase _useCase;

        public CartItemController(AddToCartUseCase useCase)
        {
            _useCase = useCase;
        }
        [HttpPost]
        public async Task<IActionResult> AddToCart(int UserId, int ProductId, int quantity)
        {
            await _useCase.addCartItem(UserId, ProductId, quantity);
            return Ok();
        }

    }
}