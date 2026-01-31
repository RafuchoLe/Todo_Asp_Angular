using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Toolbox.API.DTOs.Auth;
using Toolbox.Infrastructure.Data;

namespace Toolbox.Integration.Tests;
public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "TestDb_" + Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                //Remove the existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ToolboxDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                //Add InMemory database for testing
                services.AddDbContext<ToolboxDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_Should_Create_New_User_And_Return_Token()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "password123",
            FirstName = "Jhon",
            LastName = "Doe"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        //Assert 
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Token.Should().NotBeNullOrEmpty();
        authResponse.User.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Login_Should_Return_Token_With_Valid_Credentials()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "logintest@example.com",
            Password = "password123",
            FirstName = "Jane",
            LastName = "Doe"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "logintest@example.com",
            Password = "password123"
        };

        // Act
        var response =await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Should_Fail_With_Invalid_Credentials()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "wrongpassword"
        };
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]                                                                                                                   
    public async Task Register_Should_Return_BadRequest_For_Duplicate_Email()                                                
    {                                                                                                                        
        // Arrange                                                                                                           
        var request = new RegisterRequest                                                                                    
        {                                                                                                                    
            Email = "duplicate@example.com",                                                                                 
            Password = "password123",                                                                                        
            FirstName = "John",                                                                                              
            LastName = "Doe"                                                                                                 
        };                                                                                                                   
                                                                                                                            
        // Registrar la primera vez                                                                                          
        await _client.PostAsJsonAsync("/api/auth/register", request);                                                        
                                                                                                                            
        // Act — intentar registrar de nuevo con el mismo email                                                              
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);                                         
                                                                                                                            
        // Assert                                                                                                            
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);                                                          
    }                                                                                                             
                                                                                                                            
    [Fact]                                                                                                                   
    public async Task GetMe_Should_Return_Unauthorized_Without_Token()                                                       
    {                                                                                                                        
        // Act — hacer GET sin header Authorization                                                                          
        var response = await _client.GetAsync("/api/auth/me");                                                               
                                                                                                                            
        // Assert                                                                                                            
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);                                                        
    }                                                                                                                        
                                                                                                                                                                                                                
                                                                                                                            
    [Fact]                                                                                                                   
    public async Task GetMe_Should_Return_User_With_Valid_Token()                                                            
    {                                                                                                                        
        // Arrange — registrar un usuario para obtener un token                                                              
        var registerRequest = new RegisterRequest                                                                            
        {                                                                                                                    
            Email = "metest@example.com",                                                                                    
            Password = "password123",                                                                                        
            FirstName = "Me",                                                                                                
            LastName = "Test"                                                                                                
        };                                                                                                                   
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);                         
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();                                 
                                                                                                                            
        // Configurar el header Authorization con el token obtenido                                                          
        _client.DefaultRequestHeaders.Authorization =                                                                        
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);                            
                                                                                                                            
        // Act                                                                                                               
        var response = await _client.GetAsync("/api/auth/me");                                                               
                                                                                                                            
        // Assert                                                                                                            
        response.StatusCode.Should().Be(HttpStatusCode.OK);                                                                  
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();                                                   
        userDto.Should().NotBeNull();                                                                                        
        userDto!.Email.Should().Be(registerRequest.Email);                                                                   
    }
}