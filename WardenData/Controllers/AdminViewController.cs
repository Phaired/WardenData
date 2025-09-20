using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WardenData.Models;
using WardenData.Services;

namespace WardenData.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;

    public AdminController(AppDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        // Check if already logged in
        var token = HttpContext.Session.GetString("AdminToken");
        if (!string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Dashboard");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Le nom d'utilisateur et le mot de passe sont requis";
            return View();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive && u.Role == UserRole.Admin);

        if (user == null || !_passwordService.VerifyPassword(password, user.PasswordHash))
        {
            ViewBag.Error = "Nom d'utilisateur/mot de passe incorrect ou permissions insuffisantes";
            return View();
        }

        // Store in session
        HttpContext.Session.SetString("AdminToken", user.Token);
        HttpContext.Session.SetString("AdminUsername", user.Username);

        return RedirectToAction("Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var token = HttpContext.Session.GetString("AdminToken");
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login");
        }

        var users = await _context.Users.ToListAsync();
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword)
    {
        if (!await IsAdminAuthenticated())
            return Unauthorized();

        var token = HttpContext.Session.GetString("AdminToken");
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Token == token && u.IsActive);

        if (user == null)
        {
            return Unauthorized();
        }

        // Verify old password
        if (!_passwordService.VerifyPassword(oldPassword, user.PasswordHash))
        {
            return BadRequest("Ancien mot de passe incorrect");
        }

        // Update password
        user.PasswordHash = _passwordService.HashPassword(newPassword);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(string username, string password, UserRole role)
    {
        if (!await IsAdminAuthenticated())
            return Unauthorized();

        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            return BadRequest("Username already exists");
        }

        var token = Guid.NewGuid().ToString("N");
        while (await _context.Users.AnyAsync(u => u.Token == token))
        {
            token = Guid.NewGuid().ToString("N");
        }

        var user = new User
        {
            Username = username,
            Token = token,
            PasswordHash = _passwordService.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { user.Id, user.Username, user.Token, user.CreatedAt, user.IsActive, user.Role });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateToken(int id)
    {
        if (!await IsAdminAuthenticated())
            return Unauthorized();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        string newToken;
        do
        {
            newToken = Guid.NewGuid().ToString("N");
        }
        while (await _context.Users.AnyAsync(u => u.Token == newToken));

        user.Token = newToken;
        await _context.SaveChangesAsync();

        return Ok(new { Token = newToken });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (!await IsAdminAuthenticated())
            return Unauthorized();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok();
    }

    private async Task<bool> IsAdminAuthenticated()
    {
        var token = HttpContext.Session.GetString("AdminToken");
        if (string.IsNullOrEmpty(token))
            return false;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Token == token && u.IsActive && u.Role == UserRole.Admin);

        return user != null;
    }
}

