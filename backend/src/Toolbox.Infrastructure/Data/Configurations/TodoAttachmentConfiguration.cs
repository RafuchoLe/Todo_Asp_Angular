using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toolbox.Core.Entities;

namespace Toolbox.Infrastructure.Data.Configurations;

public class TodoAttachmentConfiguration : IEntityTypeConfiguration<TodoAttachment>
{
    public void Configure(EntityTypeBuilder<TodoAttachment> builder)
    {
        builder.ToTable("TodoAttachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.StoredPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.FileSizeBytes)
            .IsRequired();

        builder.Property(a => a.FileType)
            .IsRequired();

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
    }
}
