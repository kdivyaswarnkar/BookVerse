using BookStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Data.Configurations;

public class EmailQueueItemConfiguration : IEntityTypeConfiguration<EmailQueueItem>
{
    public void Configure(EntityTypeBuilder<EmailQueueItem> builder)
    {
        builder.ToTable("EmailQueue", "ops");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ToEmail).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Subject).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Body).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(10).IsRequired();
        builder.Property(e => e.LastError).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}