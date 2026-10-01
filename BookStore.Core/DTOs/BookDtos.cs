namespace BookStore.Core.DTOs;

// One card in the book list
public class BookListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public string CategoryName { get; set; } = "";
}

// Full details for the book page
public class BookDetailDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string? Isbn { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// Wrapper used by every list API that supports paging
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}