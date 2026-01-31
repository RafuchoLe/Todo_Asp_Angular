using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Toolbox.API.DTOs.Auth;
using Toolbox.Core.Interfaces;
using Toolbox.Infrastructure.Identity;
using System.Security.Claims;

namespace Toolbox.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtTokenGenerator _tokenGenerator;

    public AuthController(IAuthService authService, JwtTokenGenerator tokenGenerator)
    {
        _authService = authService;
        _tokenGenerator = tokenGenerator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register ( [FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName
        );

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error});
        }

        var token = _tokenGenerator.GenerateToken(result.User!);

        var response = new AuthResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = result.User!.Id,
                Email = result.User.Email,
                FirstName = result.User.FirstName,
                LastName = result.User.LastName
            }
        };

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login ( [FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(
            request.Email,
            request.Password
        );

        if (!result.Success)
        {
            return Unauthorized(new { message = result.Error});
        }

        var token = _tokenGenerator.GenerateToken(result.User!);

        var response = new AuthResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = result.User!.Id,
                Email = result.User.Email,
                FirstName = result.User.FirstName,
                LastName = result.User.LastName   
            }  
        };

        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        });
    }
}