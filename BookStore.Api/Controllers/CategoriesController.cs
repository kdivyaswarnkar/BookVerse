using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(IBookRepository books) : ControllerBase
{
    // GET api/categories
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll() =>
        Ok(await books.GetCategoriesAsync());
}