using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WardenData.Models;
using WardenData.Services;

namespace WardenData.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminApiController> _logger;
    private readonly IPasswordService _passwordService;

    public AdminApiController(AppDbContext context, ILogger<AdminApiController> logger, IPasswordService passwordService)
    {
        _context = context;
        _logger = logger;
        _passwordService = passwordService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.IsActive);

        if (user == null || user.Role != UserRole.Admin)
        {
            return Unauthorized(new { Error = "Invalid credentials or insufficient permissions" });
        }

        // Pour simplifier, on utilise le token existant de l'utilisateur
        // En production, on générerait un JWT avec expiration
        return Ok(new { Token = user.Token, Username = user.Username });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Username = u.Username,
                Token = u.Token,
                CreatedAt = u.CreatedAt,
                IsActive = u.IsActive,
                Role = u.Role
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Error = "User not found" });
        }

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Token = user.Token,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Role = user.Role
        };

        return Ok(userDto);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == createUserDto.Username))
        {
            return Conflict(new { Error = "Username already exists" });
        }

        // Generate token if not provided
        var token = !string.IsNullOrEmpty(createUserDto.Token)
            ? createUserDto.Token
            : Guid.NewGuid().ToString("N");

        // Check if token already exists
        if (await _context.Users.AnyAsync(u => u.Token == token))
        {
            return Conflict(new { Error = "Token already exists" });
        }

        var user = new User
        {
            Username = createUserDto.Username,
            Token = token,
            PasswordHash = _passwordService.HashPassword(createUserDto.Password),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Role = createUserDto.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created user {Username} with ID {UserId}", user.Username, user.Id);

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Token = user.Token,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Role = user.Role
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Error = "User not found" });
        }

        // Check if new username already exists (excluding current user)
        if (!string.IsNullOrEmpty(updateUserDto.Username) &&
            await _context.Users.AnyAsync(u => u.Username == updateUserDto.Username && u.Id != id))
        {
            return Conflict(new { Error = "Username already exists" });
        }

        // Check if new token already exists (excluding current user)
        if (!string.IsNullOrEmpty(updateUserDto.Token) &&
            await _context.Users.AnyAsync(u => u.Token == updateUserDto.Token && u.Id != id))
        {
            return Conflict(new { Error = "Token already exists" });
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(updateUserDto.Username))
            user.Username = updateUserDto.Username;

        if (!string.IsNullOrEmpty(updateUserDto.Password))
            user.PasswordHash = _passwordService.HashPassword(updateUserDto.Password);

        if (!string.IsNullOrEmpty(updateUserDto.Token))
            user.Token = updateUserDto.Token;

        if (updateUserDto.IsActive.HasValue)
            user.IsActive = updateUserDto.IsActive.Value;

        if (updateUserDto.Role.HasValue)
            user.Role = updateUserDto.Role.Value;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated user {UserId}", user.Id);

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Token = user.Token,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Role = user.Role
        };

        return Ok(userDto);
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Error = "User not found" });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted user {UserId}", id);

        return NoContent();
    }

    [HttpPost("users/{id}/regenerate-token")]
    public async Task<IActionResult> RegenerateToken(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Error = "User not found" });
        }

        string newToken;
        do
        {
            newToken = Guid.NewGuid().ToString("N");
        }
        while (await _context.Users.AnyAsync(u => u.Token == newToken));

        user.Token = newToken;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Regenerated token for user {UserId}", id);

        return Ok(new { Token = newToken });
    }
}

// DTOs
public class CreateUserDto
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;

    [MaxLength(255)]
    public string? Token { get; set; }

    public UserRole Role { get; set; } = UserRole.User;
}

public class UpdateUserDto
{
    [MaxLength(100)]
    public string? Username { get; set; }

    [MinLength(6)]
    public string? Password { get; set; }

    [MaxLength(255)]
    public string? Token { get; set; }

    public bool? IsActive { get; set; }

    public UserRole? Role { get; set; }
}

public class UserResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public UserRole Role { get; set; }
}

public class LoginDto
{
    [Required]
    public string Username { get; set; } = null!;
}