using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using WardenData.Models;
using System.Text.Json;

namespace WardenData.Controllers;

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
        // Pour chaque DTO, on crée un Order en générant un nouvel UUID pour Id
        // et en stockant l'identifiant original dans OriginalId
        var orderEntities = orders.Select(dto => new Order
        {
            Id = Guid.NewGuid(),
            OriginalId = dto.Id,
            Name = dto.Name
        }).ToList();

        return await ProcessEntities(orderEntities, _context.Orders);
    }

    [HttpPost("order-effects")]
    public async Task<IActionResult> ReceiveOrderEffects([FromBody] List<OrderEffectDTO> effectDtos)
    {
        // Récupérer les identifiants originaux des Order référencés par les OrderEffect
        var originalOrderIds = effectDtos.Select(dto => dto.OrderId).Distinct().ToList();
        var orders = await _context.Orders
            .Where(o => originalOrderIds.Contains(o.OriginalId))
            .ToDictionaryAsync(o => o.OriginalId, o => o.Id);

        // S'il manque un Order référencé, on renvoie une erreur
        foreach (var orderId in originalOrderIds)
        {
            if (!orders.ContainsKey(orderId))
            {
                return BadRequest($"Order avec OriginalId {orderId} introuvable.");
            }
        }

        // Pour chaque OrderEffect, on génère un nouvel UUID et on retrouve l'UUID du Order parent
        var effects = effectDtos.Select(dto => new OrderEffect
        {
            Id = Guid.NewGuid(),
            OriginalId = dto.Id,
            OrderId = orders[dto.OrderId],
            EffectName = dto.EffectName,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            DesiredValue = dto.DesiredValue
        }).ToList();

        return await ProcessEntities(effects, _context.OrderEffects);
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> ReceiveSessions([FromBody] List<SessionDTO> sessionDtos)
    {
        // Récupérer les identifiants originaux des Order référencés par les sessions
        var originalOrderIds = sessionDtos.Select(dto => dto.OrderId).Distinct().ToList();
        var orders = await _context.Orders
            .Where(o => originalOrderIds.Contains(o.OriginalId))
            .ToDictionaryAsync(o => o.OriginalId, o => o.Id);

        foreach (var orderId in originalOrderIds)
        {
            if (!orders.ContainsKey(orderId))
            {
                return BadRequest($"Order avec OriginalId {orderId} introuvable.");
            }
        }

        var sessions = sessionDtos.Select(dto => new Session
        {
            Id = Guid.NewGuid(),
            OriginalId = dto.Id,
            OrderId = orders[dto.OrderId],
            Timestamp = dto.Timestamp,
            InitialEffects = dto.InitialEffects,
            RunesPrices = dto.RunesPrices
        }).ToList();

        return await ProcessEntities(sessions, _context.Sessions);
    }

    [HttpPost("rune-history")]
    public async Task<IActionResult> ReceiveRuneHistory([FromBody] List<RuneHistoryDTO> historyDtos)
    {
        // Récupérer les identifiants originaux des Sessions référencées par les RuneHistory
        var originalSessionIds = historyDtos.Select(dto => dto.SessionId).Distinct().ToList();
        var sessions = await _context.Sessions
            .Where(s => originalSessionIds.Contains(s.OriginalId))
            .ToDictionaryAsync(s => s.OriginalId, s => s.Id);

        foreach (var sessionId in originalSessionIds)
        {
            if (!sessions.ContainsKey(sessionId))
            {
                return BadRequest($"Session avec OriginalId {sessionId} introuvable.");
            }
        }

        var histories = historyDtos.Select(dto => new RuneHistory
        {
            Id = Guid.NewGuid(),
            OriginalId = dto.Id,
            SessionId = sessions[dto.SessionId],
            RuneId = dto.RuneId,
            IsTenta = dto.IsTenta,
            EffectsAfter = dto.EffectsAfter.ToString(),
            HasSucceed = dto.HasSucceed
        }).ToList();

        return await ProcessEntities(histories, _context.RuneHistories);
    }


    private async Task<IActionResult> ProcessEntities<T>(List<T> entities, DbSet<T> dbSet) where T : class
    {
        try
        {
            await _context.BulkInsertOrUpdateAsync(entities);
            return Ok(new { Received = entities.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors du traitement de {typeof(T).Name}");
            return StatusCode(500, $"Erreur lors du traitement de {typeof(T).Name}");
        }
    }
}

// Les DTO restent inchangés (ils utilisent les id d'origine)
public class SessionDTO
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public long Timestamp { get; set; }
    
    [Required]
    public string InitialEffects { get; set; }
    
    [Required]
    public string RunesPrices { get; set; }
}

public class RuneHistoryDTO
{
    public int Id { get; set; }

    [JsonPropertyName("session_id")]
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
