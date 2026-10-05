using BookStore.Api.Extensions;
using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]   // private data
public class WishlistController(IWishlistService wishlist) : ControllerBase
{
    // GET api/wishlist
    [HttpGet]
    public async Task<ActionResult<List<WishlistItemDto>>> List() =>
        Ok(await wishlist.ListAsync(User.GetUserId()));

    // PUT api/wishlist/1   (add; safe to call twice)
    [HttpPut("{bookId:int}")]
    public async Task<IActionResult> Add(int bookId)
    {
        await wishlist.AddAsync(User.GetUserId(), bookId);
        return NoContent();
    }

    // DELETE api/wishlist/1   (remove; safe to call twice)
    [HttpDelete("{bookId:int}")]
    public async Task<IActionResult> Remove(int bookId)
    {
        await wishlist.RemoveAsync(User.GetUserId(), bookId);
        return NoContent();
    }
}
