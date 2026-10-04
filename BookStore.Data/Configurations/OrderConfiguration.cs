using BookStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", "sales");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.TotalAmount).HasPrecision(10, 2);
        builder.Property(o => o.DiscountAmount).HasPrecision(10, 2);
        builder.Property(o => o.Status).HasMaxLength(20).IsRequired();
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(o => o.Address)
            .WithMany()
            .HasForeignKey(o => o.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", "sales");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.UnitPrice).HasPrecision(10, 2);

        builder.HasOne(i => i.Book)
            .WithMany()
            .HasForeignKey(i => i.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}