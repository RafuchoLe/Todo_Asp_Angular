using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Toolbox.Core.Entities;

namespace Toolbox.Core.Tests.Entities;

public class UserTests
{
    [Fact]
    public void User_Should_Inherit_From_BaseEntity()
    {
        // Arrange & Act
        var user = new User();

        //Assert
        user.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void User_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hashedPassword",
            FirstName = "Jhon",
            LastName = "Doe"
        };

        // Assert
        user.Email.Should().Be("test@example.com");
        user.PasswordHash.Should().Be("hashedPassword");
        user.FirstName.Should().Be("Jhon");
        user.LastName.Should().Be("Doe");
    }

    [Fact]
    public void User_Should_Initialize_Empty_TodoItems_Collection()
    {
        // Arrange & Act
        var user = new User();  
    
        // Assert
        user.TodoItems.Should().NotBeNull();
        user.TodoItems.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "Should fail for empty email")]
    [InlineData("", "Should fail for null email")]
    public void Use_Email_Should_Be_Required(string email, string reason)
    {
        // Arrange
        var user = new User();
        
        // Act & Assert
        user.Email.Should().BeNullOrEmpty(reason);
    }
}