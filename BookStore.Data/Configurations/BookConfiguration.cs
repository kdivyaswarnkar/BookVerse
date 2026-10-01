using BookStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // catalog.Books has a trigger (price history), so EF must not use the OUTPUT clause
        builder.ToTable("Books", "catalog", t => t.UseSqlOutputClause(false));

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Author).HasMaxLength(150).IsRequired();
        builder.Property(b => b.Isbn).HasMaxLength(20);
        builder.Property(b => b.ImageUrl).HasMaxLength(500);
        builder.Property(b => b.Price).HasPrecision(10, 2);
        builder.Property(b => b.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}