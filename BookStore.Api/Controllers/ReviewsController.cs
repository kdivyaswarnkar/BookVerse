using BookStore.Api.Extensions;
using BookStore.Core.Constants;
using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BookStore.Api.Controllers;

[ApiController]
public class ReviewsController(IReviewService reviews, IOutputCacheStore outputCache) : ControllerBase
{
    // GET api/books/1/reviews?page=1&pageSize=10   (public)
    [HttpGet("api/books/{bookId:int}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<BookReviewsDto>> List(int bookId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        return Ok(await reviews.ListAsync(bookId, page, pageSize));
    }

    // PUT api/books/1/reviews   { "rating": 5, "comment": "Loved it" }   (add or edit MY review)
    [HttpPut("api/books/{bookId:int}/reviews")]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> Save(int bookId, SaveReviewRequest request, CancellationToken ct)
    {
        var dto = await reviews.SaveAsync(User.GetUserId(), bookId, request);
        await outputCache.EvictByTagAsync("books", ct);   // book lists show the average rating
        return Ok(dto);
    }

    // DELETE api/books/1/reviews   (delete MY review)
    [HttpDelete("api/books/{bookId:int}/reviews")]
    [Authorize]
    public async Task<IActionResult> DeleteMine(int bookId, CancellationToken ct)
    {
        await reviews.DeleteMineAsync(User.GetUserId(), bookId);
        await outputCache.EvictByTagAsync("books", ct);
        return NoContent();
    }

    // DELETE api/admin/reviews/5   (admin removes any review, e.g. abuse)
    [HttpDelete("api/admin/reviews/{reviewId:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> DeleteAsAdmin(int reviewId, CancellationToken ct)
    {
        await reviews.DeleteAsAdminAsync(reviewId);
        await outputCache.EvictByTagAsync("books", ct);
        return NoContent();
    }
}
