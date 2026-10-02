using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookStore.Api.Controllers;

// Every cart call needs a logged-in user, and works only on THAT user's cart.
[ApiController]
[Route("api/cart")]
[Authorize]
// private data: tell browsers and proxies never to store these responses
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CartController(ICartService cart) : ControllerBase
{
    // GET api/cart
    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart() =>
        Ok(await cart.GetCartAsync(UserId));

    // POST api/cart/items      { "bookId": 5, "quantity": 2 }
    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(AddToCartRequest request) =>
        Ok(await cart.AddItemAsync(UserId, request));

    // PUT api/cart/items/5     { "quantity": 3 }
    [HttpPut("items/{bookId:int}")]
    public async Task<ActionResult<CartDto>> UpdateItem(int bookId, UpdateCartItemRequest request) =>
        Ok(await cart.UpdateItemAsync(UserId, bookId, request.Quantity));

    // DELETE api/cart/items/5
    [HttpDelete("items/{bookId:int}")]
    public async Task<IActionResult> RemoveItem(int bookId)
    {
        await cart.RemoveItemAsync(UserId, bookId);
        return NoContent();
    }

    // DELETE api/cart
    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        await cart.ClearAsync(UserId);
        return NoContent();
    }

    // The user id always comes from the token, never from the URL or the body,
    // so one customer can never read or change another customer's cart.
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}