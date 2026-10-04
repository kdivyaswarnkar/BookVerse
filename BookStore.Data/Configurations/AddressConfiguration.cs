using BookStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Data.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses", "auth");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FullName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Phone).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Line1).HasMaxLength(200).IsRequired();
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.State).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(a => a.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}