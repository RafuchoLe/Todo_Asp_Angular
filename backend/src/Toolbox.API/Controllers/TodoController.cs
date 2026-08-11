using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Toolbox.API.DTOs.Todo;
using Toolbox.Core.Entities;
using Toolbox.Core.Interfaces;

namespace Toolbox.API.Controllers;

[ApiController]
[Route("api/todo")]
[Authorize]
public class TodoController : ControllerBase
{
    private readonly ITodoService _todoService;
    private readonly IFileService _fileService;

    public TodoController(ITodoService todoService, IFileService fileService)
    {
        _todoService = todoService;
        _fileService = fileService;
    }

    // GET /api/todo
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItemDto>>> GetAll()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var todos = await _todoService.GetAllByUserAsync(userId.Value);
        return Ok(todos.Select(t => MapToDto(t)));
    }

    // GET /api/todo/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoItemDto>> GetById(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var todo = await _todoService.GetByIdAsync(id, userId.Value);
        if (todo == null) return NotFound();

        return Ok(MapToDto(todo));
    }

    // POST /api/todo
    [HttpPost]
    public async Task<ActionResult<TodoItemDto>> Create([FromBody] CreateTodoRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var todo = new TodoItem
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            DueDate = request.DueDate,
            UserId = userId.Value
        };

        var created = await _todoService.CreateAsync(todo);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    // PUT /api/todo/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTodoRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var todo = new TodoItem
        {
            Id = id,
            Title = request.Title,
            Description = request.Description,
            IsCompleted = request.IsCompleted,
            Priority = request.Priority,
            DueDate = request.DueDate
        };

        var (success, error) = await _todoService.UpdateAsync(todo, userId.Value);
        if (!success) return NotFound(new { message = error });

        return NoContent();
    }

    // DELETE /api/todo/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var attachments = await _todoService.GetAttachmentsAsync(id, userId.Value);
        var (success, error) = await _todoService.DeleteAsync(id, userId.Value);
        if (!success) return NotFound(new { message = error });

        foreach (var attachment in attachments)
            await _fileService.DeleteFileAsync(attachment.StoredPath);

        return NoContent();
    }

    // POST /api/todo/{todoId}/attachments
    [HttpPost("{todoId:guid}/attachments")]
    public async Task<ActionResult<IEnumerable<TodoAttachmentDto>>> UploadAttachments(
        Guid todoId, [FromForm] List<IFormFile> files)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (files.Count == 0) return BadRequest(new { message = "No files provided" });
        if (files.Count > 5) return BadRequest(new { message = "Maximum 5 files per request" });

        var fileDataList = files.Select(f => new FileUploadData(f.OpenReadStream(), f.FileName, f.ContentType, f.Length)).ToList();

        var invalidFile = fileDataList.FirstOrDefault(f => !_fileService.IsAllowedFile(f));
        if (invalidFile != null)
            return BadRequest(new { message = $"File '{invalidFile.FileName}' is not allowed (invalid type or size)" });

        var results = new List<TodoAttachmentDto>();
        foreach (var fileData in fileDataList)
        {
            var attachment = await _fileService.SaveFileAsync(fileData, todoId);
            var saved = await _todoService.AddAttachmentAsync(todoId, userId.Value, attachment);
            results.Add(MapAttachmentToDto(saved, todoId));
        }

        return StatusCode(201, results);
    }

    // GET /api/todo/{todoId}/attachments
    [HttpGet("{todoId:guid}/attachments")]
    public async Task<ActionResult<IEnumerable<TodoAttachmentDto>>> GetAttachments(Guid todoId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var attachments = await _todoService.GetAttachmentsAsync(todoId, userId.Value);
        return Ok(attachments.Select(a => MapAttachmentToDto(a, todoId)));
    }

    // GET /api/todo/{todoId}/attachments/{attachmentId}/download
    [HttpGet("{todoId:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid todoId, Guid attachmentId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var attachments = await _todoService.GetAttachmentsAsync(todoId, userId.Value);
        var attachment = attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment == null) return NotFound();

        var result = await _fileService.GetFileAsync(attachment.StoredPath, attachment.FileName, attachment.ContentType);
        if (result == null) return NotFound();

        return File(result.Value.FileStream, result.Value.ContentType, result.Value.FileName);
    }

    // DELETE /api/todo/{todoId}/attachments/{attachmentId}
    [HttpDelete("{todoId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid todoId, Guid attachmentId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var (success, error, storedPath) = await _todoService.RemoveAttachmentAsync(attachmentId, todoId, userId.Value);
        if (!success) return NotFound(new { message = error });

        if (storedPath != null)
            await _fileService.DeleteFileAsync(storedPath);

        return NoContent();
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private TodoItemDto MapToDto(TodoItem todo) => new()
    {
        Id = todo.Id,
        Title = todo.Title,
        Description = todo.Description,
        IsCompleted = todo.IsCompleted,
        Priority = todo.Priority,
        DueDate = todo.DueDate,
        CreatedAt = todo.CreatedAt,
        UpdatedAt = todo.UpdatedAt,
        Attachments = todo.Attachments.Select(a => MapAttachmentToDto(a, todo.Id)).ToList()
    };

    private TodoAttachmentDto MapAttachmentToDto(TodoAttachment a, Guid todoId) => new()
    {
        Id = a.Id,
        FileName = a.FileName,
        ContentType = a.ContentType,
        FileSizeBytes = a.FileSizeBytes,
        FileType = a.FileType,
        CreatedAt = a.CreatedAt,
        DownloadUrl = $"/api/todo/{todoId}/attachments/{a.Id}/download"
    };
}
