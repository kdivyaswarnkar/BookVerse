using BookStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Data.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons", "sales");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(30).IsRequired();
        builder.Property(c => c.DiscountType).HasMaxLength(10).IsRequired();
        builder.Property(c => c.DiscountValue).HasPrecision(10, 2);
        builder.Property(c => c.MinOrderAmount).HasPrecision(10, 2);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(c => c.Code).IsUnique();   // UQ_Coupons_Code
    }
}
