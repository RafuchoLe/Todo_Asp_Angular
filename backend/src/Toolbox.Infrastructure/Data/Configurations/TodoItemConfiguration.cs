using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toolbox.Core.Entities;
using Toolbox.Core.Enums;

namespace Toolbox.Infrastructure.Data.Configurations;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("TodoItems");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasSentinel(Priority.Low)
            .HasDefaultValue(Priority.Medium);

        builder.Property(t => t.DueDate);

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasMany(t => t.Attachments)
            .WithOne(a => a.TodoItem)
            .HasForeignKey(a => a.TodoItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
