namespace Toolbox.Core.Entities;

public class TodoItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User? User { get; set; } = null!;
}