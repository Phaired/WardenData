
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WardenData.Controllers;

using WardenData.Models;
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DataController> _logger;

    public DataController(AppDbContext context, ILogger<DataController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("orders")]
    public async Task<IActionResult> ReceiveOrders([FromBody] List<OrderDTO> orders)
    {
        // Convert OrderDTOs to Order entities if needed
        var orderEntities = orders.Select(dto => new Order { Id = dto.Id, Name = dto.Name }).ToList();
        return await ProcessEntities(orderEntities, _context.Orders);
    }

    [HttpPost("order-effects")]
    public async Task<IActionResult> ReceiveOrderEffects(
        [FromBody] List<OrderEffectDTO> effectDtos)
    {
        var effects = effectDtos.Select(dto => new OrderEffect 
        {
            Id = dto.Id,
            OrderId = dto.OrderId,  // Direct mapping to foreign key
            EffectName = dto.EffectName,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            DesiredValue = dto.DesiredValue
        }).ToList();

        return await ProcessEntities(effects, _context.OrderEffects);
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> ReceiveSessions(
        [FromBody] List<SessionDTO> sessionDtos)
    {
        // Convert DTOs to Session entities
        var sessions = sessionDtos.Select(dto => new Session {
            Id = dto.Id,
            OrderId = dto.OrderId,
            Timestamp = dto.Timestamp,
            InitialEffects = dto.InitialEffects,
            RunesPrices = dto.RunesPrices
        }).ToList();

        // Explicitly specify the generic type parameter
        return await ProcessEntities<Session>(sessions, _context.Sessions);
    }

    [HttpPost("rune-history")]
    public async Task<IActionResult> ReceiveRuneHistory(
        [FromBody] List<RuneHistoryDTO> historyDtos)
    {
        var histories = historyDtos.Select(dto => new RuneHistory
        {
            Id = dto.Id,
            SessionId = dto.SessionId,
            RuneId = dto.RuneId,
            IsTenta = dto.IsTenta,
            EffectsAfter = dto.EffectsAfter.ToString(),
            HasSucceed = dto.HasSucceed
        }).ToList();

        return await ProcessEntities(histories, _context.RuneHistories);
    }

    private async Task<IActionResult> ProcessEntities<T>(
        List<T> entities, 
        DbSet<T> dbSet) where T : class
    {
        try
        {
            await _context.BulkInsertOrUpdateAsync(entities);
            return Ok(new { Received = entities.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing {typeof(T).Name}");
            return StatusCode(500, $"Error processing {typeof(T).Name}");
        }
    }
}

// DTO Classes
public class SessionDTO
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public long Timestamp { get; set; }
    
    [Required]  // Add validation attribute
    public string InitialEffects { get; set; }
    
    [Required]  // Add validation attribute
    public string RunesPrices { get; set; }
}
public class RuneHistoryDTO
{
    public int Id { get; set; }

    [JsonPropertyName("session_id")] // Match Rust's snake_case
    public int SessionId { get; set; }

    [JsonPropertyName("rune_id")]
    public int RuneId { get; set; }

    [JsonPropertyName("is_tenta")]
    public bool IsTenta { get; set; }

    [JsonPropertyName("effects_after")]
    public JsonElement EffectsAfter { get; set; }

    [JsonPropertyName("has_succeed")]
    public bool HasSucceed { get; set; }
}

public class OrderDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    // Removed the Effects collection
}

public class OrderEffectDTO
{
    public int Id { get; set; }
    
    [JsonPropertyName("OrderId")]
    public int OrderId { get; set; }
    
    [JsonPropertyName("EffectName")]
    public string EffectName { get; set; }
    
    [JsonPropertyName("MinValue")]
    public long MinValue { get; set; }
    
    [JsonPropertyName("MaxValue")]
    public long MaxValue { get; set; }
    
    [JsonPropertyName("DesiredValue")]
    public long DesiredValue { get; set; }
}