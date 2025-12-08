using Toolbox.Core.Entities;

namespace Toolbox.Core.Interfaces;

public interface IAuthService
{
    Task<(bool Success, User? User, string? Error)> RegisterAsync(string email, string password, string firstName, string lastName);
    Task<(bool Success, User? User, string? Error)> LoginAsync(string email, string password);
    Task<User?> GetUserByIdAsync(Guid userId);
}
