using Microsoft.EntityFrameworkCore;
using WardenData.Models;

namespace WardenData.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;

    public AuthService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByTokenAsync(string token)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Token == token && u.IsActive);
    }

    public async Task<bool> IsAdminAsync(string token)
    {
        var user = await GetUserByTokenAsync(token);
        return user?.Role == UserRole.Admin;
    }
}