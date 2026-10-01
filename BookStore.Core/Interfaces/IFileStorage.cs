namespace BookStore.Core.Interfaces;

public interface IFileStorage
{
    // Saves the image and returns the public URL, for example /uploads/books/abc123.jpg
    Task<string> SaveBookImageAsync(Stream content, string extension, CancellationToken ct = default);

    Task DeleteAsync(string url);
}