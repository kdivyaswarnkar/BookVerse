using BookStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Data.Configurations;

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("Wishlist", "sales");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(w => new { w.UserId, w.BookId }).IsUnique();

        builder.HasOne(w => w.Book).WithMany().HasForeignKey(w => w.BookId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
