using BookStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", "catalog");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Comment).HasMaxLength(1000);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // matches UQ_Reviews_Book_User in SQL: one row per user per book (even if soft-deleted)
        builder.HasIndex(r => new { r.BookId, r.UserId }).IsUnique();

        builder.HasOne<Book>().WithMany().HasForeignKey(r => r.BookId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);

        // deleted reviews are invisible everywhere unless a query says IgnoreQueryFilters()
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
