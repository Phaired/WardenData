using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using WardenData.Models;
using WardenData.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddOpenApi();

// Add Redis cache
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "WardenData";
});

// Add database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Add queue and background services
builder.Services.AddSingleton<IQueueService<QueueItem>, QueueService<QueueItem>>();
builder.Services.AddScoped<IDataConverter, DataConverter>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddHostedService<DataProcessingBackgroundService>();

// Add authentication and authorization
builder.Services.AddAuthentication("Token")
    .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>("Token", null);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

// Database migration logic
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
    Console.WriteLine("Database migrations applied successfully");

    // Seed default user if no users exist
    if (!dbContext.Users.Any())
    {
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var defaultUser = new User
        {
            Username = "adm",
            PasswordHash = passwordService.HashPassword("adm123"),
            Token = Guid.NewGuid().ToString(),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        dbContext.Users.Add(defaultUser);
        dbContext.SaveChanges();
        Console.WriteLine($"Default admin user created - Username: adm, Token: {defaultUser.Token}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error applying migrations: {ex.Message}");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Login}/{id?}");

app.Run();