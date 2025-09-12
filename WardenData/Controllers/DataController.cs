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

        // Récupérer ou créer les Effects basés sur les noms
        var effectNames = effectDtos.Select(dto => dto.EffectName).Distinct().ToList();
        var existingEffects = await _context.Effects
            .Where(e => effectNames.Contains(e.Name))
            .ToDictionaryAsync(e => e.Name, e => e.Id);

        // Créer les Effects manquants
        var missingEffects = effectNames.Where(name => !existingEffects.ContainsKey(name)).ToList();
        if (missingEffects.Any())
        {
            var newEffects = missingEffects.Select(name => new Effect
            {
                Code = NormalizeEffectName(name),
                Name = name,
                IsPercent = name.Contains("%")
            }).ToList();
            
            await _context.BulkInsertAsync(newEffects);
            
            // Recharger pour récupérer les IDs
            var reloadedEffects = await _context.Effects
                .Where(e => missingEffects.Contains(e.Name))
                .ToDictionaryAsync(e => e.Name, e => e.Id);
            
            foreach (var kvp in reloadedEffects)
                existingEffects[kvp.Key] = kvp.Value;
        }

        // Pour chaque OrderEffect, on génère un nouvel UUID et on retrouve l'UUID du Order parent
        var effects = effectDtos.Select(dto => new OrderEffect
        {
            Id = Guid.NewGuid(),
            OriginalId = dto.Id,
            OrderId = orders[dto.OrderId],
            EffectId = existingEffects[dto.EffectName],
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            DesiredValue = dto.DesiredValue
        }).ToList();

        return await ProcessEntities(effects, _context.OrderEffects);
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> ReceiveSessions([FromBody] List<SessionDTO> sessionDtos)
    {
        try
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

            var sessions = new List<Session>();
            var sessionInitialEffects = new List<SessionInitialEffect>();
            var sessionRunePrices = new List<SessionRunePrice>();

            foreach (var dto in sessionDtos)
            {
                var sessionId = Guid.NewGuid();
                var session = new Session
                {
                    Id = sessionId,
                    OriginalId = dto.Id,
                    OrderId = orders[dto.OrderId],
                    Timestamp = dto.Timestamp,
                    StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(dto.Timestamp).DateTime
                };
                sessions.Add(session);

                // Parser et traiter InitialEffects JSON
                var initialEffectsJson = JsonDocument.Parse(dto.InitialEffects);
                if (initialEffectsJson.RootElement.TryGetProperty("effects", out var effectsArray) ||
                    initialEffectsJson.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var effectsToProcess = effectsArray.ValueKind == JsonValueKind.Array ? effectsArray : initialEffectsJson.RootElement;
                    await ProcessSessionEffects(effectsToProcess, sessionId, sessionInitialEffects);
                }

                // Parser et traiter RunesPrices JSON  
                var runesPricesJson = JsonDocument.Parse(dto.RunesPrices);
                if (runesPricesJson.RootElement.ValueKind == JsonValueKind.Array)
                {
                    await ProcessSessionRunePrices(runesPricesJson.RootElement, sessionId, sessionRunePrices);
                }
            }

            // Insérer tout en bulk
            await _context.BulkInsertAsync(sessions);
            await _context.BulkInsertAsync(sessionInitialEffects);
            await _context.BulkInsertAsync(sessionRunePrices);

            return Ok(new { Received = sessionDtos.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du traitement des sessions");
            return StatusCode(500, "Erreur lors du traitement des sessions");
        }
    }

    [HttpPost("rune-history")]
    public async Task<IActionResult> ReceiveRuneHistory([FromBody] List<RuneHistoryDTO> historyDtos)
    {
        try
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

            // S'assurer que les runes existent
            var runeIds = historyDtos.Select(dto => dto.RuneId).Distinct().ToList();
            var existingRunes = await _context.Runes
                .Where(r => runeIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToHashSetAsync();

            var missingRunes = runeIds.Where(id => !existingRunes.Contains(id)).ToList();
            if (missingRunes.Any())
            {
                var newRunes = missingRunes.Select(id => new Rune
                {
                    Id = id,
                    Name = $"Rune {id}" // Nom par défaut
                }).ToList();
                
                await _context.BulkInsertAsync(newRunes);
            }

            var histories = new List<RuneHistory>();
            var effectChanges = new List<RuneHistoryEffectChange>();

            // Traiter chaque RuneHistory
            foreach (var dto in historyDtos)
            {
                var historyId = Guid.NewGuid();
                var history = new RuneHistory
                {
                    Id = historyId,
                    OriginalId = dto.Id,
                    SessionId = sessions[dto.SessionId],
                    RuneId = dto.RuneId,
                    IsTenta = dto.IsTenta,
                    HasSucceed = dto.HasSucceed,
                    AppliedAt = DateTime.UtcNow
                };
                histories.Add(history);

                // Parser et traiter les effects_after JSON
                await ProcessRuneHistoryEffects(dto.EffectsAfter, historyId, dto.SessionId, dto.Id, effectChanges);
            }

            // Insérer tout en bulk
            await _context.BulkInsertAsync(histories);
            await _context.BulkInsertAsync(effectChanges);

            return Ok(new { Received = historyDtos.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du traitement de l'historique des runes");
            return StatusCode(500, "Erreur lors du traitement de l'historique des runes");
        }
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

    private string NormalizeEffectName(string effectName)
    {
        return effectName
            .ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("%", "_pct")
            .Replace("é", "e")
            .Replace("è", "e")
            .Replace("à", "a")
            .Replace("ç", "c");
    }

    private async Task ProcessSessionEffects(JsonElement effectsArray, Guid sessionId, List<SessionInitialEffect> sessionInitialEffects)
    {
        var effectNames = new List<string>();
        
        foreach (var effectElement in effectsArray.EnumerateArray())
        {
            if (effectElement.TryGetProperty("effect_name", out var effectNameProp))
            {
                effectNames.Add(effectNameProp.GetString());
            }
        }

        // Récupérer ou créer les Effects
        var effectsMap = await GetOrCreateEffects(effectNames.Distinct().ToList());

        foreach (var effectElement in effectsArray.EnumerateArray())
        {
            if (effectElement.TryGetProperty("effect_name", out var effectNameProp) &&
                effectElement.TryGetProperty("current_value", out var valueProp))
            {
                var effectName = effectNameProp.GetString();
                var value = valueProp.GetInt32();

                sessionInitialEffects.Add(new SessionInitialEffect
                {
                    SessionId = sessionId,
                    EffectId = effectsMap[effectName],
                    Value = value
                });
            }
        }
    }

    private async Task ProcessSessionRunePrices(JsonElement runesArray, Guid sessionId, List<SessionRunePrice> sessionRunePrices)
    {
        var runeIds = new List<int>();
        
        foreach (var runeElement in runesArray.EnumerateArray())
        {
            if (runeElement.TryGetProperty("id", out var idProp))
            {
                runeIds.Add(idProp.GetInt32());
            }
        }

        // S'assurer que les runes existent
        await EnsureRunesExist(runeIds);

        foreach (var runeElement in runesArray.EnumerateArray())
        {
            if (runeElement.TryGetProperty("id", out var idProp) &&
                runeElement.TryGetProperty("price", out var priceProp))
            {
                sessionRunePrices.Add(new SessionRunePrice
                {
                    SessionId = sessionId,
                    RuneId = idProp.GetInt32(),
                    Price = priceProp.GetInt32()
                });
            }
        }
    }

    private async Task ProcessRuneHistoryEffects(JsonElement effectsAfter, Guid runeHistoryId, int sessionOriginalId, int stepId, List<RuneHistoryEffectChange> effectChanges)
    {
        JsonElement effectsArray;
        
        if (effectsAfter.ValueKind == JsonValueKind.Array)
        {
            effectsArray = effectsAfter;
        }
        else if (effectsAfter.TryGetProperty("effects", out var effects))
        {
            effectsArray = effects;
        }
        else
        {
            return; // Pas d'effets à traiter
        }

        var effectNames = new List<string>();
        
        foreach (var effectElement in effectsArray.EnumerateArray())
        {
            if (effectElement.TryGetProperty("effect_name", out var effectNameProp))
            {
                effectNames.Add(effectNameProp.GetString());
            }
        }

        // Récupérer ou créer les Effects
        var effectsMap = await GetOrCreateEffects(effectNames.Distinct().ToList());

        // Récupérer les valeurs précédentes pour calculer les deltas
        var previousValues = await GetPreviousEffectValues(sessionOriginalId, stepId, effectsMap.Values.ToList());

        foreach (var effectElement in effectsArray.EnumerateArray())
        {
            if (effectElement.TryGetProperty("effect_name", out var effectNameProp) &&
                effectElement.TryGetProperty("current_value", out var valueProp))
            {
                var effectName = effectNameProp.GetString();
                var newValue = valueProp.GetInt32();
                var effectId = effectsMap[effectName];

                var oldValue = previousValues.TryGetValue(effectId, out var prevVal) ? prevVal : (int?)null;

                // Ne stocker que si la valeur a changé
                if (oldValue != newValue)
                {
                    effectChanges.Add(new RuneHistoryEffectChange
                    {
                        RuneHistoryId = runeHistoryId,
                        EffectId = effectId,
                        OldValue = oldValue,
                        NewValue = newValue
                    });
                }
            }
        }
    }

    private async Task<Dictionary<string, short>> GetOrCreateEffects(List<string> effectNames)
    {
        var existingEffects = await _context.Effects
            .Where(e => effectNames.Contains(e.Name))
            .ToDictionaryAsync(e => e.Name, e => e.Id);

        var missingEffects = effectNames.Where(name => !existingEffects.ContainsKey(name)).ToList();
        
        if (missingEffects.Any())
        {
            var newEffects = missingEffects.Select(name => new Effect
            {
                Code = NormalizeEffectName(name),
                Name = name,
                IsPercent = name.Contains("%")
            }).ToList();
            
            await _context.BulkInsertAsync(newEffects);
            
            // Recharger pour récupérer les IDs
            var reloadedEffects = await _context.Effects
                .Where(e => missingEffects.Contains(e.Name))
                .ToDictionaryAsync(e => e.Name, e => e.Id);
            
            foreach (var kvp in reloadedEffects)
                existingEffects[kvp.Key] = kvp.Value;
        }

        return existingEffects;
    }

    private async Task EnsureRunesExist(List<int> runeIds)
    {
        var existingRunes = await _context.Runes
            .Where(r => runeIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToHashSetAsync();

        var missingRunes = runeIds.Where(id => !existingRunes.Contains(id)).ToList();
        
        if (missingRunes.Any())
        {
            var newRunes = missingRunes.Select(id => new Rune
            {
                Id = id,
                Name = $"Rune {id}"
            }).ToList();
            
            await _context.BulkInsertAsync(newRunes);
        }
    }

    private async Task<Dictionary<short, int>> GetPreviousEffectValues(int sessionOriginalId, int currentStepId, List<short> effectIds)
    {
        // Récupérer les valeurs initiales de la session
        var sessionId = await _context.Sessions
            .Where(s => s.OriginalId == sessionOriginalId)
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        var initialValues = await _context.SessionInitialEffects
            .Where(sie => sie.SessionId == sessionId && effectIds.Contains(sie.EffectId))
            .ToDictionaryAsync(sie => sie.EffectId, sie => sie.Value);

        // Si c'est le premier step, retourner les valeurs initiales
        if (currentStepId == 1)
        {
            return initialValues;
        }

        // Sinon, récupérer la dernière valeur connue depuis les changements précédents
        var latestChanges = await _context.RuneHistoryEffectChanges
            .Join(_context.RuneHistories, rhec => rhec.RuneHistoryId, rh => rh.Id, (rhec, rh) => new { rhec, rh })
            .Where(joined => joined.rh.SessionId == sessionId && 
                           joined.rh.OriginalId < currentStepId &&
                           effectIds.Contains(joined.rhec.EffectId))
            .GroupBy(joined => joined.rhec.EffectId)
            .Select(g => g.OrderByDescending(x => x.rh.OriginalId).First())
            .ToDictionaryAsync(x => x.rhec.EffectId, x => x.rhec.NewValue);

        // Combiner : utiliser les derniers changements s'ils existent, sinon les valeurs initiales
        var result = new Dictionary<short, int>();
        foreach (var effectId in effectIds)
        {
            if (latestChanges.TryGetValue(effectId, out var latestValue))
            {
                result[effectId] = latestValue;
            }
            else if (initialValues.TryGetValue(effectId, out var initialValue))
            {
                result[effectId] = initialValue;
            }
        }

        return result;
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
