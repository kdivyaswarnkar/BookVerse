using BookStore.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Book> Books => Set<Book>();
    // each new ticket adds its DbSet here (Users, Orders, ...)

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // picks up every IEntityTypeConfiguration<T> class in this project
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}