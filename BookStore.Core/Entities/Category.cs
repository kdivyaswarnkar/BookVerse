namespace BookStore.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();
}