using Toolbox.Core.Entities;

namespace Toolbox.Core.Interfaces;

public interface ITodoService
{
    Task<IEnumerable<TodoItem>> GetAllByUserAsync(Guid userId);
    Task<TodoItem?> GetByIdAsync(Guid todoId, Guid userId);
    Task<TodoItem> CreateAsync(TodoItem todoItem);
    Task<(bool Success, string? Error)> UpdateAsync(TodoItem todoItem, Guid userId);
    Task<(bool Success, string? Error)> DeleteAsync(Guid todoId, Guid userId);
    Task<TodoAttachment> AddAttachmentAsync(Guid todoId, Guid userId, TodoAttachment attachment);
    Task<(bool Success, string? Error, string? StoredPath)> RemoveAttachmentAsync(Guid attachmentId, Guid todoId, Guid userId);
    Task<IEnumerable<TodoAttachment>> GetAttachmentsAsync(Guid todoId, Guid userId);
}
