using System.ComponentModel.DataAnnotations;
using Toolbox.Core.Enums;

namespace Toolbox.Core.Entities;

public class TodoAttachment : BaseEntity
{
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string StoredPath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public FileType FileType { get; set; }

    public Guid TodoItemId { get; set; }
    public TodoItem? TodoItem { get; set; } = null!;
}
