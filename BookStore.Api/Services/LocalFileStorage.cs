using BookStore.Core.Interfaces;

namespace BookStore.Api.Services;

// Saves images under wwwroot/uploads/books. For deployment on a free host the disk is temporary,
// so this class is later replaced by a cloud version (same IFileStorage interface).
public class LocalFileStorage(IWebHostEnvironment env) : IFileStorage
{
    private const string Folder = "uploads/books";

    private string WebRoot => env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");

    public async Task<string> SaveBookImageAsync(Stream content, string extension, CancellationToken ct = default)
    {
        var directory = Path.Combine(WebRoot, Folder);
        Directory.CreateDirectory(directory);

        // random name: the client never chooses the file name or path
        var fileName = $"{Guid.NewGuid():N}{extension}";

        await using var file = File.Create(Path.Combine(directory, fileName));
        await content.CopyToAsync(file, ct);

        return $"/{Folder}/{fileName}";
    }

    public Task DeleteAsync(string url)
    {
        // GetFileName strips any folders, so only files inside our uploads folder can be deleted
        var path = Path.Combine(WebRoot, Folder, Path.GetFileName(url));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}