using BookStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", "sales");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Provider).HasMaxLength(30).IsRequired();
        builder.Property(p => p.ProviderPaymentId).HasMaxLength(100);
        builder.Property(p => p.Amount).HasPrecision(10, 2);
        builder.Property(p => p.Status).HasMaxLength(20).IsRequired();
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // one payment record per order
        builder.HasOne<Order>()
            .WithOne()
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}