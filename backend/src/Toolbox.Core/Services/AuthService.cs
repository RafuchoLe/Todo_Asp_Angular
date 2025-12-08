using Toolbox.Core.Entities;
using Toolbox.Core.Interfaces;

namespace Toolbox.Core.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    
    public AuthService(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<(bool Success, User? User, string? Error)> RegisterAsync(
        string email, string password, string firstName, string lastName)
    {
        // Check if email already exists
        var emailExists = await _userRepository.ExistsAsync(u => u.Email == email);
        if (emailExists)
        {
            return (false, null, "Email already registered");
        }
        
        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        // Create user
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName
        };
        
        var createdUser = await _userRepository.AddAsync(user);
        
        return (true, createdUser, null);
    }
    
    public async Task<(bool Success, User? User, string? Error)> LoginAsync(string email, string password)
    {
        var user = await _userRepository.FirstOrDefaultAsync(u => u.Email == email);
        
        if (user == null)
        {
            return (false, null, "Invalid credentials");
        }
        
        // Verify password
        var passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        
        if (!passwordValid)
        {
            return (false, null, "Invalid credentials");
        }
        
        return (true, user, null);
    }
    
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }
}
