
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
    public async Task<IActionResult> ReceiveOrders([FromBody] List<Order> orders)
    {
        return await ProcessEntities(orders, _context.Orders);
    }

    [HttpPost("order-effects")]
    public async Task<IActionResult> ReceiveOrderEffects(
        [FromBody] List<OrderEffect> effects)
    {
        return await ProcessEntities(effects, _context.OrderEffects);
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> ReceiveSessions(
        [FromBody] List<SessionDTO> sessionDtos)
    {
        var sessions = sessionDtos.Select(dto => new Session
        {
            Id = dto.Id,
            OrderId = dto.OrderId,
            Timestamp = dto.Timestamp,
            InitialEffects = JsonSerializer.Serialize(dto.InitialEffects),
            RunesPrices = JsonSerializer.Serialize(dto.RunesPrices)
        }).ToList();

        return await ProcessEntities(sessions, _context.Sessions);
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
            EffectsAfter = JsonSerializer.Serialize(dto.EffectsAfter),
            HasSucceed = dto.HasSucceed
        }).ToList();

        return await ProcessEntities(histories, _context.RuneHistories);
    }

    private async Task<IActionResult> ProcessEntities<T>(List<T> entities, DbSet<T> dbSet) 
        where T : class
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
    public JsonElement InitialEffects { get; set; }
    public JsonElement RunesPrices { get; set; }
}

public class RuneHistoryDTO
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int RuneId { get; set; }
    public bool IsTenta { get; set; }
    public JsonElement EffectsAfter { get; set; }
    public bool HasSucceed { get; set; }
}