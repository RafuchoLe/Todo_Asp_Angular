using FluentAssertions;
using Toolbox.Core.Entities;
using Toolbox.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace Toolbox.Core.Tests.Services;

public class JwtTokenGeneratorTests
{
    private readonly JwtTokenGenerator _tokenGenerator;
    
    public JwtTokenGeneratorTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"JwtSettings:SecretKey", "YourSuperSecretKeyMustBeAtLeast32CharactersLong!"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"},
                {"JwtSettings:ExpirationMinutes", "60"}
            }!)
            .Build();
            
        _tokenGenerator = new JwtTokenGenerator(configuration);
    }
    
    [Fact]
    public void GenerateToken_Should_Return_Valid_Jwt()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        
        // Act
        var token = _tokenGenerator.GenerateToken(user);
        
        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }
    
    [Fact]
    public void GenerateToken_Should_Have_Correct_Expiration()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com" };
        
        // Act
        var token = _tokenGenerator.GenerateToken(user);
        
        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
    }
}