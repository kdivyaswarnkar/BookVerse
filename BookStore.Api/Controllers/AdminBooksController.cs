using System.Security.Claims;
using BookStore.Core.Constants;
using BookStore.Core.DTOs;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/admin/books")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminBooksController(
    IBookAdminRepository admin,
    IBookRepository books,
    IFileStorage storage,
    IOutputCacheStore outputCache) : ControllerBase
{
    private const long MaxImageBytes = 2 * 1024 * 1024;   // 2 MB
    private const string CacheTag = "books";

    // POST api/admin/books
    [HttpPost]
    public async Task<IActionResult> Create(BookUpsertRequest request, CancellationToken ct)
    {
        var id = await admin.AddAsync(request, AdminId);
        await outputCache.EvictByTagAsync(CacheTag, ct);   // cached book lists are now out of date
        return CreatedAtAction("Get", "Books", new { id }, new { id });
    }

    // PUT api/admin/books/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, BookUpsertRequest request, CancellationToken ct)
    {
        if (!await admin.UpdateAsync(id, request, AdminId)) return NotFound();
        await outputCache.EvictByTagAsync(CacheTag, ct);
        return NoContent();
    }

    // DELETE api/admin/books/5   (soft delete)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await admin.DeleteAsync(id, AdminId)) return NotFound();
        await outputCache.EvictByTagAsync(CacheTag, ct);
        return NoContent();
    }

    // POST api/admin/books/5/image   (multipart form with one file)
    [HttpPost("{id:int}/image")]
    [RequestSizeLimit(MaxImageBytes + 64 * 1024)]   // stop huge uploads before they are read
    public async Task<IActionResult> UploadImage(int id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new AppException("Choose an image file.", 400);
        if (file.Length > MaxImageBytes)
            throw new AppException("Image must be 2 MB or smaller.", 400);

        var book = await books.GetByIdAsync(id);
        if (book is null) return NotFound();

        await using var stream = file.OpenReadStream();

        // decide the type from the file's first bytes, never from its name or the Content-Type header
        var header = new byte[12];
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
        var extension = DetectImageExtension(header.AsSpan(0, read));
        if (extension is null)
            throw new AppException("Only JPG, PNG or WEBP images are allowed.", 400);

        stream.Position = 0;
        var url = await storage.SaveBookImageAsync(stream, extension, ct);

        await admin.SetImageAsync(id, url, AdminId);
        if (!string.IsNullOrEmpty(book.ImageUrl))
            await storage.DeleteAsync(book.ImageUrl);   // remove the old picture

        await outputCache.EvictByTagAsync(CacheTag, ct);
        return Ok(new { imageUrl = url });
    }

    private static string? DetectImageExtension(ReadOnlySpan<byte> h)
    {
        ReadOnlySpan<byte> png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
        if (h.Length >= 8 && h[..8].SequenceEqual(png)) return ".png";
        if (h.Length >= 12 && h[..4].SequenceEqual("RIFF"u8) && h.Slice(8, 4).SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }

    private int AdminId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}