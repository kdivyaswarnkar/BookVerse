using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(IBookRepository books) : ControllerBase
{
    // GET api/categories
    // HTTP CACHE HEADERS: adds "Cache-Control: public,max-age=300", so the browser (or a CDN)
    // reuses the answer for 5 minutes without calling the server at all.
    // The server side also has its own in-memory cache (CachedBookRepository).
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<List<CategoryDto>>> GetAll() =>
        Ok(await books.GetCategoriesAsync());
}