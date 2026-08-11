using Toolbox.Core.Entities;
using Toolbox.Core.Interfaces;

namespace Toolbox.Core.Services;

public class TodoService : ITodoService
{
    private readonly IRepository<TodoItem> _todoRepo;
    private readonly IRepository<TodoAttachment> _attachmentRepo;

    public TodoService(IRepository<TodoItem> todoRepo, IRepository<TodoAttachment> attachmentRepo)
    {
        _todoRepo = todoRepo;
        _attachmentRepo = attachmentRepo;
    }

    public async Task<IEnumerable<TodoItem>> GetAllByUserAsync(Guid userId)
    {
        return await _todoRepo.FindAsync(t => t.UserId == userId);
    }

    public async Task<TodoItem?> GetByIdAsync(Guid todoId, Guid userId)
    {
        return await _todoRepo.FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);
    }

    public async Task<TodoItem> CreateAsync(TodoItem todoItem)
    {
        return await _todoRepo.AddAsync(todoItem);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(TodoItem todoItem, Guid userId)
    {
        var existing = await _todoRepo.FirstOrDefaultAsync(t => t.Id == todoItem.Id && t.UserId == userId);
        if (existing == null)
            return (false, "Todo not found");

        existing.Title = todoItem.Title;
        existing.Description = todoItem.Description;
        existing.IsCompleted = todoItem.IsCompleted;
        existing.Priority = todoItem.Priority;
        existing.DueDate = todoItem.DueDate;

        await _todoRepo.UpdateAsync(existing);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid todoId, Guid userId)
    {
        var todo = await _todoRepo.FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);
        if (todo == null)
            return (false, "Todo not found");

        await _todoRepo.DeleteAsync(todo);
        return (true, null);
    }

    public async Task<TodoAttachment> AddAttachmentAsync(Guid todoId, Guid userId, TodoAttachment attachment)
    {
        var todo = await _todoRepo.FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId)
            ?? throw new InvalidOperationException("Todo not found");

        attachment.TodoItemId = todo.Id;
        return await _attachmentRepo.AddAsync(attachment);
    }

    public async Task<(bool Success, string? Error, string? StoredPath)> RemoveAttachmentAsync(
        Guid attachmentId, Guid todoId, Guid userId)
    {
        var todo = await _todoRepo.FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);
        if (todo == null)
            return (false, "Todo not found", null);

        var attachment = await _attachmentRepo.FirstOrDefaultAsync(
            a => a.Id == attachmentId && a.TodoItemId == todoId);
        if (attachment == null)
            return (false, "Attachment not found", null);

        var storedPath = attachment.StoredPath;
        await _attachmentRepo.DeleteAsync(attachment);
        return (true, null, storedPath);
    }

    public async Task<IEnumerable<TodoAttachment>> GetAttachmentsAsync(Guid todoId, Guid userId)
    {
        var todo = await _todoRepo.FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);
        if (todo == null)
            return [];

        return await _attachmentRepo.FindAsync(a => a.TodoItemId == todoId);
    }
}
