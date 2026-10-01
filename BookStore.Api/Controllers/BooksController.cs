using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController(IBookRepository books) : ControllerBase
{
    // GET api/books?q=sample&categoryId=1&page=1&pageSize=12
    [HttpGet]
    public async Task<ActionResult<PagedResult<BookListItemDto>>> Search(
        [FromQuery] string? q,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);   // never let a client ask for thousands
        return Ok(await books.SearchAsync(q, categoryId, page, pageSize));
    }

    // GET api/books/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookDetailDto>> Get(int id)
    {
        var book = await books.GetByIdAsync(id);
        return book is null ? NotFound() : Ok(book);
    }
}