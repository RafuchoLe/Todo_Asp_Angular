using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using Toolbox.Core.Entities;
using Toolbox.Core.Enums;
using Toolbox.Core.Interfaces;
using Toolbox.Core.Services;

namespace Toolbox.Core.Tests.Services;

public class TodoServiceTests
{
    private readonly Mock<IRepository<TodoItem>> _todoRepoMock;
    private readonly Mock<IRepository<TodoAttachment>> _attachmentRepoMock;
    private readonly TodoService _service;

    public TodoServiceTests()
    {
        _todoRepoMock = new Mock<IRepository<TodoItem>>();
        _attachmentRepoMock = new Mock<IRepository<TodoAttachment>>();
        _service = new TodoService(_todoRepoMock.Object, _attachmentRepoMock.Object);
    }

    // --- GetAllByUser ---

    [Fact]
    public async Task GetAllByUser_Should_Return_Only_User_Todos()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var todos = new List<TodoItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Task 1", UserId = userId },
            new() { Id = Guid.NewGuid(), Title = "Task 2", UserId = userId }
        };

        _todoRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()))
            .ReturnsAsync(todos);

        // Act
        var result = await _service.GetAllByUserAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        _todoRepoMock.Verify(r => r.FindAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()), Times.Once);
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_Should_Return_Todo_When_Owner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        var todo = new TodoItem { Id = todoId, Title = "My Todo", UserId = userId };

        _todoRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()))
            .ReturnsAsync(todo);

        // Act
        var result = await _service.GetByIdAsync(todoId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(todoId);
    }

    [Fact]
    public async Task GetById_Should_Return_Null_For_Other_Users_Todo()
    {
        // Arrange
        _todoRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()))
            .ReturnsAsync((TodoItem?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    // --- Create ---

    [Fact]
    public async Task Create_Should_Set_UserId_And_Save()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var todo = new TodoItem { Title = "New Todo", UserId = userId };

        _todoRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TodoItem>()))
            .ReturnsAsync((TodoItem t) => t);

        // Act
        var result = await _service.CreateAsync(todo);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Todo");
        result.UserId.Should().Be(userId);
        _todoRepoMock.Verify(r => r.AddAsync(It.IsAny<TodoItem>()), Times.Once);
    }

    // --- Update ---

    [Fact]
    public async Task Update_Should_Succeed_When_Owner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existing = new TodoItem { Id = Guid.NewGuid(), Title = "Old", UserId = userId };
        var updated = new TodoItem { Id = existing.Id, Title = "New", Description = "Desc", IsCompleted = true, Priority = Priority.High };

        _todoRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()))
            .ReturnsAsync(existing);

        // Act
        var (success, error) = await _service.UpdateAsync(updated, userId);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
        existing.Title.Should().Be("New");
        existing.IsCompleted.Should().BeTrue();
        _todoRepoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task Update_Should_Fail_For_Nonexistent_Todo()
    {
        // Arrange
        _todoRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()))
            .ReturnsAsync((TodoItem?)null);

        // Act
        var (success, error) = await _service.UpdateAsync(new TodoItem { Id = Guid.NewGuid() }, Guid.NewGuid());

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Todo not found");
        _todoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<TodoItem>()), Times.Never);
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_Should_Succeed_When_Owner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var todo = new TodoItem { Id = Guid.NewGuid(), Title = "Task", UserId = userId };

        _todoRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()))
            .ReturnsAsync(todo);

        // Act
        var (success, error) = await _service.DeleteAsync(todo.Id, userId);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
        _todoRepoMock.Verify(r => r.DeleteAsync(todo), Times.Once);
    }

    [Fact]
    public async Task Delete_Should_Fail_For_Other_Users_Todo()
    {
        // Arrange
        _todoRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()))
            .ReturnsAsync((TodoItem?)null);

        // Act
        var (success, error) = await _service.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Todo not found");
        _todoRepoMock.Verify(r => r.DeleteAsync(It.IsAny<TodoItem>()), Times.Never);
    }

    // --- AddAttachment ---

    [Fact]
    public async Task AddAttachment_Should_Save_Attachment_To_Repository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var todo = new TodoItem { Id = Guid.NewGuid(), Title = "Task", UserId = userId };
        var attachment = new TodoAttachment { FileName = "img.jpg", ContentType = "image/jpeg" };

        _todoRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()))
            .ReturnsAsync(todo);

        _attachmentRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TodoAttachment>()))
            .ReturnsAsync((TodoAttachment a) => a);

        // Act
        var result = await _service.AddAttachmentAsync(todo.Id, userId, attachment);

        // Assert
        result.Should().NotBeNull();
        result.TodoItemId.Should().Be(todo.Id);
        _attachmentRepoMock.Verify(r => r.AddAsync(It.IsAny<TodoAttachment>()), Times.Once);
    }

    [Fact]
    public async Task AddAttachment_Should_Fail_For_Other_Users_Todo()
    {
        // Arrange
        _todoRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()))
            .ReturnsAsync((TodoItem?)null);

        // Act
        var act = async () => await _service.AddAttachmentAsync(Guid.NewGuid(), Guid.NewGuid(), new TodoAttachment());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Todo not found");
        _attachmentRepoMock.Verify(r => r.AddAsync(It.IsAny<TodoAttachment>()), Times.Never);
    }

    // --- RemoveAttachment ---

    [Fact]
    public async Task RemoveAttachment_Should_Delete_And_Return_StoredPath()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var todo = new TodoItem { Id = Guid.NewGuid(), UserId = userId };
        var attachment = new TodoAttachment
        {
            Id = Guid.NewGuid(),
            TodoItemId = todo.Id,
            StoredPath = "Uploads/todo-attachments/abc.jpg"
        };

        _todoRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()))
            .ReturnsAsync(todo);

        _attachmentRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoAttachment, bool>>>()))
            .ReturnsAsync(attachment);

        // Act
        var (success, error, storedPath) = await _service.RemoveAttachmentAsync(attachment.Id, todo.Id, userId);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
        storedPath.Should().Be("Uploads/todo-attachments/abc.jpg");
        _attachmentRepoMock.Verify(r => r.DeleteAsync(attachment), Times.Once);
    }

    [Fact]
    public async Task RemoveAttachment_Should_Fail_For_Nonexistent_Attachment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var todo = new TodoItem { Id = Guid.NewGuid(), UserId = userId };

        _todoRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()))
            .ReturnsAsync(todo);

        _attachmentRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoAttachment, bool>>>()))
            .ReturnsAsync((TodoAttachment?)null);

        // Act
        var (success, error, storedPath) = await _service.RemoveAttachmentAsync(Guid.NewGuid(), todo.Id, userId);

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Attachment not found");
        storedPath.Should().BeNull();
        _attachmentRepoMock.Verify(r => r.DeleteAsync(It.IsAny<TodoAttachment>()), Times.Never);
    }

    // --- GetAttachments ---

    [Fact]
    public async Task GetAttachments_Should_Return_Attachments_For_Owned_Todo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var todo = new TodoItem { Id = Guid.NewGuid(), UserId = userId };
        var attachments = new List<TodoAttachment>
        {
            new() { Id = Guid.NewGuid(), TodoItemId = todo.Id, FileName = "a.jpg" },
            new() { Id = Guid.NewGuid(), TodoItemId = todo.Id, FileName = "b.mp4" }
        };

        _todoRepoMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TodoItem, bool>>>()))
            .ReturnsAsync(todo);

        _attachmentRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TodoAttachment, bool>>>()))
            .ReturnsAsync(attachments);

        // Act
        var result = await _service.GetAttachmentsAsync(todo.Id, userId);

        // Assert
        result.Should().HaveCount(2);
    }
}
