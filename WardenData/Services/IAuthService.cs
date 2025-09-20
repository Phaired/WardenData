using WardenData.Models;

namespace WardenData.Services;

public interface IAuthService
{
    Task<User?> GetUserByTokenAsync(string token);
    Task<bool> IsAdminAsync(string token);
}