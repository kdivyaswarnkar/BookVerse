using BookStore.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailQueueItem> EmailQueue => Set<EmailQueueItem>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    // each new ticket adds its DbSet here (Payments, ...)

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // picks up every IEntityTypeConfiguration<T> class in this project
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}