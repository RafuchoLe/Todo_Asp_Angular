using System.ComponentModel.DataAnnotations;
using Toolbox.Core.Enums;

namespace Toolbox.API.DTOs.Todo;

public class CreateTodoRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public Priority Priority { get; set; } = Priority.Medium;

    public DateTime? DueDate { get; set; }
}
