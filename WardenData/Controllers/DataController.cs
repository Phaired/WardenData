
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace WardenData.Controllers;

using WardenData.Models;
using WardenData.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly IDistributedCache _cache;
    private readonly IQueueService<QueueItem> _queueService;
    private readonly ILogger<DataController> _logger;

    public DataController(
        IDistributedCache cache,
        IQueueService<QueueItem> queueService,
        ILogger<DataController> logger)
    {
        _cache = cache;
        _queueService = queueService;
        _logger = logger;
    }

    [HttpPost("orders")]
    public async Task<IActionResult> ReceiveOrders([FromBody] List<OrderDTO> orders)
    {
        var trackingId = Guid.NewGuid().ToString();
        _logger.LogInformation("Received {Count} orders with tracking ID {TrackingId}", orders.Count, trackingId);

        try
        {
            // Cache the data
            var jsonData = JsonSerializer.Serialize(orders);
            await _cache.SetStringAsync(trackingId, jsonData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            // Queue for processing
            await _queueService.EnqueueAsync(new OrderQueueItem { Id = trackingId });

            return Accepted(new { TrackingId = trackingId, Received = orders.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching orders data");
            return StatusCode(500, "Error processing request");
        }
    }

    [HttpPost("order-effects")]
    public async Task<IActionResult> ReceiveOrderEffects([FromBody] List<OrderEffectDTO> effectDtos)
    {
        var trackingId = Guid.NewGuid().ToString();
        _logger.LogInformation("Received {Count} order effects with tracking ID {TrackingId}", effectDtos.Count, trackingId);

        try
        {
            // Cache the data
            var jsonData = JsonSerializer.Serialize(effectDtos);
            await _cache.SetStringAsync(trackingId, jsonData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            // Queue for processing
            await _queueService.EnqueueAsync(new OrderEffectQueueItem { Id = trackingId });

            return Accepted(new { TrackingId = trackingId, Received = effectDtos.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching order effects data");
            return StatusCode(500, "Error processing request");
        }
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> ReceiveSessions([FromBody] List<SessionDTO> sessionDtos)
    {
        var trackingId = Guid.NewGuid().ToString();
        _logger.LogInformation("Received {Count} sessions with tracking ID {TrackingId}", sessionDtos.Count, trackingId);

        try
        {
            // Cache the data
            var jsonData = JsonSerializer.Serialize(sessionDtos);
            await _cache.SetStringAsync(trackingId, jsonData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            // Queue for processing
            await _queueService.EnqueueAsync(new SessionQueueItem { Id = trackingId });

            return Accepted(new { TrackingId = trackingId, Received = sessionDtos.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching sessions data");
            return StatusCode(500, "Error processing request");
        }
    }

    [HttpPost("rune-history")]
    public async Task<IActionResult> ReceiveRuneHistory([FromBody] List<RuneHistoryDTO> historyDtos)
    {
        var trackingId = Guid.NewGuid().ToString();
        _logger.LogInformation("Received {Count} rune histories with tracking ID {TrackingId}", historyDtos.Count, trackingId);

        try
        {
            // Cache the data
            var jsonData = JsonSerializer.Serialize(historyDtos);
            await _cache.SetStringAsync(trackingId, jsonData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            // Queue for processing
            await _queueService.EnqueueAsync(new RuneHistoryQueueItem { Id = trackingId });

            return Accepted(new { TrackingId = trackingId, Received = historyDtos.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching rune history data");
            return StatusCode(500, "Error processing request");
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