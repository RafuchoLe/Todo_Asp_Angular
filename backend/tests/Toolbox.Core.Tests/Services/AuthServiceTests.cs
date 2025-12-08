using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Toolbox.Core.Entities;
using Toolbox.Core.Interfaces;
using Toolbox.Core.Services;

namespace Toolbox.Core.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IRepository<User>>();
        _authService = new AuthService(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_Should_Create_New_User_With_Hashed_Password()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123";
        var firstName = "Jhon";
        var lastName = "Doe";

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _authService.RegisterAsync(email, password, firstName, lastName);

        // Assert
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(email);
        result.User.FirstName.Should().Be(firstName);
        result.User.LastName.Should().Be(lastName);
        result.User.PasswordHash.Should().NotBe(password); // Should be hashed

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_Sould_Fail_If_Email_Already_Exists()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterAsync("test@example.com", "password", "jhon", "Doe");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Email already registered");
        result.User.Should().BeNull();

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_User_If_Credentials_Are_Valid_Credentials()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var existingUser = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            FirstName = "Jhon",
            LastName = "Doe"
        };

        _userRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(email);
    }

    [Fact]
    public async Task LoginAsync_Should_Fail_With_Invalid_Email()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);

        //Act
        var result = await _authService.LoginAsync("wrong@example.com", "password");

        //Asssert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_Should_Fail_With_Invalid_Password()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
        };

        _userRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        //Act
        var result = await _authService.LoginAsync("test@example.com", "wrongpassword");

        //Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials");
    }
    
}