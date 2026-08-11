using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Toolbox.API.Controllers;
using Toolbox.API.DTOs.Todo;
using Toolbox.Core.Entities;
using Toolbox.Core.Enums;
using Toolbox.Core.Interfaces;

namespace Toolbox.API.Tests.Controllers;

public class TodoControllerTests
{
    private readonly Mock<ITodoService> _todoServiceMock;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly TodoController _controller;
    private readonly Guid _userId;

    public TodoControllerTests()
    {
        _todoServiceMock = new Mock<ITodoService>();
        _fileServiceMock = new Mock<IFileService>();
        _controller = new TodoController(_todoServiceMock.Object, _fileServiceMock.Object);
        _userId = Guid.NewGuid();

        SetUser(_userId);
    }

    private void SetUser(Guid userId)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private void SetNoUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    // --- GetAll ---

    [Fact]
    public async Task GetAll_Should_Return_Ok_With_TodoList()
    {
        // Arrange
        var todos = new List<TodoItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Task 1", UserId = _userId, Attachments = [] },
            new() { Id = Guid.NewGuid(), Title = "Task 2", UserId = _userId, Attachments = [] }
        };
        _todoServiceMock.Setup(s => s.GetAllByUserAsync(_userId)).ReturnsAsync(todos);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = ok.Value.Should().BeAssignableTo<IEnumerable<TodoItemDto>>().Subject;
        dtos.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_Should_Return_Unauthorized_Without_Claim()
    {
        SetNoUser();
        var result = await _controller.GetAll();
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_Should_Return_Ok_When_Found()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todo = new TodoItem { Id = todoId, Title = "Task", UserId = _userId, Attachments = [] };
        _todoServiceMock.Setup(s => s.GetByIdAsync(todoId, _userId)).ReturnsAsync(todo);

        // Act
        var result = await _controller.GetById(todoId);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<TodoItemDto>().Which.Id.Should().Be(todoId);
    }

    [Fact]
    public async Task GetById_Should_Return_NotFound()
    {
        // Arrange
        _todoServiceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), _userId)).ReturnsAsync((TodoItem?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid());

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // --- Create ---

    [Fact]
    public async Task Create_Should_Return_Created()
    {
        // Arrange
        var request = new CreateTodoRequest { Title = "New Task", Priority = Priority.High };
        var created = new TodoItem { Id = Guid.NewGuid(), Title = "New Task", UserId = _userId, Attachments = [] };
        _todoServiceMock.Setup(s => s.CreateAsync(It.IsAny<TodoItem>())).ReturnsAsync(created);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().BeOfType<TodoItemDto>().Which.Title.Should().Be("New Task");
    }

    // --- Update ---

    [Fact]
    public async Task Update_Should_Return_NoContent_On_Success()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var request = new UpdateTodoRequest { Title = "Updated", IsCompleted = true };
        _todoServiceMock.Setup(s => s.UpdateAsync(It.IsAny<TodoItem>(), _userId))
            .ReturnsAsync((true, (string?)null));

        // Act
        var result = await _controller.Update(todoId, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Should_Return_NotFound()
    {
        // Arrange
        _todoServiceMock.Setup(s => s.UpdateAsync(It.IsAny<TodoItem>(), _userId))
            .ReturnsAsync((false, "Todo not found"));

        // Act
        var result = await _controller.Update(Guid.NewGuid(), new UpdateTodoRequest { Title = "x" });

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_Should_Return_NoContent_On_Success()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        _todoServiceMock.Setup(s => s.GetAttachmentsAsync(todoId, _userId))
            .ReturnsAsync([]);
        _todoServiceMock.Setup(s => s.DeleteAsync(todoId, _userId))
            .ReturnsAsync((true, (string?)null));

        // Act
        var result = await _controller.Delete(todoId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Should_Return_NotFound()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        _todoServiceMock.Setup(s => s.GetAttachmentsAsync(todoId, _userId)).ReturnsAsync([]);
        _todoServiceMock.Setup(s => s.DeleteAsync(todoId, _userId))
            .ReturnsAsync((false, "Todo not found"));

        // Act
        var result = await _controller.Delete(todoId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_Should_Delete_Physical_Files_Of_Attachments()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var attachments = new List<TodoAttachment>
        {
            new() { Id = Guid.NewGuid(), StoredPath = "Uploads/a.jpg" },
            new() { Id = Guid.NewGuid(), StoredPath = "Uploads/b.mp4" }
        };
        _todoServiceMock.Setup(s => s.GetAttachmentsAsync(todoId, _userId)).ReturnsAsync(attachments);
        _todoServiceMock.Setup(s => s.DeleteAsync(todoId, _userId)).ReturnsAsync((true, (string?)null));
        _fileServiceMock.Setup(f => f.DeleteFileAsync(It.IsAny<string>())).ReturnsAsync(true);

        // Act
        await _controller.Delete(todoId);

        // Assert
        _fileServiceMock.Verify(f => f.DeleteFileAsync("Uploads/a.jpg"), Times.Once);
        _fileServiceMock.Verify(f => f.DeleteFileAsync("Uploads/b.mp4"), Times.Once);
    }

    // --- UploadAttachments ---

    [Fact]
    public async Task UploadAttachment_Should_Return_BadRequest_For_No_Files()
    {
        // Act
        var result = await _controller.UploadAttachments(Guid.NewGuid(), []);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadAttachment_Should_Return_BadRequest_For_Invalid_File()
    {
        // Arrange
        var headers = new HeaderDictionary { ["Content-Type"] = "application/octet-stream" };
        var file = new FormFile(Stream.Null, 0, 100, "files", "virus.exe") { Headers = headers };
        _fileServiceMock.Setup(f => f.IsAllowedFile(It.IsAny<FileUploadData>())).Returns(false);

        // Act
        var result = await _controller.UploadAttachments(Guid.NewGuid(), [file]);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadAttachment_Should_Return_BadRequest_For_More_Than_5_Files()
    {
        // Arrange
        var files = Enumerable.Range(0, 6)
            .Select(_ => (IFormFile)new FormFile(Stream.Null, 0, 1, "files", "a.jpg"))
            .ToList();

        // Act
        var result = await _controller.UploadAttachments(Guid.NewGuid(), files);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // --- GetAttachments ---

    [Fact]
    public async Task ListAttachments_Should_Return_Ok_With_AttachmentList()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var attachments = new List<TodoAttachment>
        {
            new() { Id = Guid.NewGuid(), FileName = "img.jpg", ContentType = "image/jpeg", TodoItemId = todoId }
        };
        _todoServiceMock.Setup(s => s.GetAttachmentsAsync(todoId, _userId)).ReturnsAsync(attachments);

        // Act
        var result = await _controller.GetAttachments(todoId);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = ok.Value.Should().BeAssignableTo<IEnumerable<TodoAttachmentDto>>().Subject;
        dtos.Should().HaveCount(1);
    }

    // --- DeleteAttachment ---

    [Fact]
    public async Task DeleteAttachment_Should_Return_NoContent_On_Success()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        _todoServiceMock.Setup(s => s.RemoveAttachmentAsync(attachmentId, todoId, _userId))
            .ReturnsAsync((true, (string?)null, "Uploads/a.jpg"));
        _fileServiceMock.Setup(f => f.DeleteFileAsync("Uploads/a.jpg")).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteAttachment(todoId, attachmentId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _fileServiceMock.Verify(f => f.DeleteFileAsync("Uploads/a.jpg"), Times.Once);
    }

    [Fact]
    public async Task DeleteAttachment_Should_Return_NotFound()
    {
        // Arrange
        _todoServiceMock.Setup(s => s.RemoveAttachmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), _userId))
            .ReturnsAsync((false, "Attachment not found", (string?)null));

        // Act
        var result = await _controller.DeleteAttachment(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
