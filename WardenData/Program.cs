using Microsoft.EntityFrameworkCore;
using WardenData.Models;
using WardenData.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
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
builder.Services.AddHostedService<DataProcessingBackgroundService>();

var app = builder.Build();

// Database migration logic
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
    Console.WriteLine("Database migrations applied successfully");
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

app.UseAuthorization();
app.MapControllers();

app.Run();