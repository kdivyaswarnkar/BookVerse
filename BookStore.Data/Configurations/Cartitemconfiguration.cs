using BookStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Data.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems", "sales");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // one row per user per book: adding the same book again increases the quantity
        builder.HasIndex(c => new { c.UserId, c.BookId }).IsUnique();

        builder.HasOne(c => c.Book)
            .WithMany()
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}