using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Toolbox.API.DTOs.Auth;
using Toolbox.API.DTOs.Todo;
using Toolbox.Core.Enums;
using Toolbox.Infrastructure.Data;

namespace Toolbox.Integration.Tests;

public class TodoControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TodoControllerTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "TodoTestDb_" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ToolboxDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<ToolboxDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        });
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private async Task<(HttpClient client, string token)> CreateAuthenticatedClientAsync(
        string email = "user@example.com", string password = "password123")
    {
        var client = CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password,
            FirstName = "Test",
            LastName = "User"
        });
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return (client, auth.Token);
    }

    // --- CRUD ---

    [Fact]
    public async Task Create_Should_Return_Created_Todo()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("create@example.com");
        var request = new CreateTodoRequest { Title = "My first todo", Priority = Priority.High };

        // Act
        var response = await client.PostAsJsonAsync("/api/todo", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<TodoItemDto>();
        dto.Should().NotBeNull();
        dto!.Title.Should().Be("My first todo");
        dto.Priority.Should().Be(Priority.High);
        dto.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_Should_Return_Only_Own_Todos()
    {
        // Arrange — dos usuarios, cada uno crea un todo
        var (clientA, _) = await CreateAuthenticatedClientAsync("userA@example.com");
        var (clientB, _) = await CreateAuthenticatedClientAsync("userB@example.com");

        await clientA.PostAsJsonAsync("/api/todo", new CreateTodoRequest { Title = "Todo de A" });
        await clientB.PostAsJsonAsync("/api/todo", new CreateTodoRequest { Title = "Todo de B" });

        // Act — usuario A consulta sus todos
        var response = await clientA.GetAsync("/api/todo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<List<TodoItemDto>>();
        todos.Should().HaveCount(1);
        todos![0].Title.Should().Be("Todo de A");
    }

    [Fact]
    public async Task GetById_Should_Return_Todo()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("getbyid@example.com");
        var created = await (await client.PostAsJsonAsync("/api/todo",
            new CreateTodoRequest { Title = "Specific Todo" }))
            .Content.ReadFromJsonAsync<TodoItemDto>();

        // Act
        var response = await client.GetAsync($"/api/todo/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<TodoItemDto>();
        dto!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Update_Should_Modify_Todo()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("update@example.com");
        var created = await (await client.PostAsJsonAsync("/api/todo",
            new CreateTodoRequest { Title = "Original" }))
            .Content.ReadFromJsonAsync<TodoItemDto>();

        var updateRequest = new UpdateTodoRequest
        {
            Title = "Updated",
            Description = "New description",
            IsCompleted = true,
            Priority = Priority.Low
        };

        // Act
        var updateResponse = await client.PutAsJsonAsync($"/api/todo/{created!.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/todo/{created.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<TodoItemDto>();
        updated!.Title.Should().Be("Updated");
        updated.IsCompleted.Should().BeTrue();
        updated.Description.Should().Be("New description");
    }

    [Fact]
    public async Task Delete_Should_Remove_Todo()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("delete@example.com");
        var created = await (await client.PostAsJsonAsync("/api/todo",
            new CreateTodoRequest { Title = "To Delete" }))
            .Content.ReadFromJsonAsync<TodoItemDto>();

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/todo/{created!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await client.GetAsync($"/api/todo/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoints_Should_Return_Unauthorized_Without_Token()
    {
        var client = CreateClient();

        var responses = await Task.WhenAll(
            client.GetAsync("/api/todo"),
            client.PostAsJsonAsync("/api/todo", new CreateTodoRequest { Title = "x" }),
            client.PutAsJsonAsync($"/api/todo/{Guid.NewGuid()}", new UpdateTodoRequest { Title = "x" }),
            client.DeleteAsync($"/api/todo/{Guid.NewGuid()}")
        );

        foreach (var r in responses)
            r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Attachments ---

    [Fact]
    public async Task Upload_Invalid_Extension_Should_Return_BadRequest()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("badfile@example.com");
        var created = await (await client.PostAsJsonAsync("/api/todo",
            new CreateTodoRequest { Title = "Todo with attachment" }))
            .Content.ReadFromJsonAsync<TodoItemDto>();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0x00, 0x01]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "files", "malware.exe");

        // Act
        var response = await client.PostAsync($"/api/todo/{created!.Id}/attachments", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAttachments_Should_Return_Empty_List_For_New_Todo()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("getatt@example.com");
        var created = await (await client.PostAsJsonAsync("/api/todo",
            new CreateTodoRequest { Title = "Todo" }))
            .Content.ReadFromJsonAsync<TodoItemDto>();

        // Act
        var response = await client.GetAsync($"/api/todo/{created!.Id}/attachments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var attachments = await response.Content.ReadFromJsonAsync<List<TodoAttachmentDto>>();
        attachments.Should().BeEmpty();
    }

    [Fact]
    public async Task Cannot_Access_Other_Users_Todos()
    {
        // Arrange
        var (clientA, _) = await CreateAuthenticatedClientAsync("ownerA@example.com");
        var (clientB, _) = await CreateAuthenticatedClientAsync("ownerB@example.com");

        var created = await (await clientA.PostAsJsonAsync("/api/todo",
            new CreateTodoRequest { Title = "Private todo" }))
            .Content.ReadFromJsonAsync<TodoItemDto>();

        // Act — usuario B intenta acceder al todo de A
        var response = await clientB.GetAsync($"/api/todo/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cannot_Delete_Other_Users_Todo()
    {
        // Arrange
        var (clientA, _) = await CreateAuthenticatedClientAsync("delA@example.com");
        var (clientB, _) = await CreateAuthenticatedClientAsync("delB@example.com");

        var created = await (await clientA.PostAsJsonAsync("/api/todo",
            new CreateTodoRequest { Title = "Protected" }))
            .Content.ReadFromJsonAsync<TodoItemDto>();

        // Act — usuario B intenta eliminar el todo de A
        var response = await clientB.DeleteAsync($"/api/todo/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
