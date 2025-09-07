using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WardenData.Models;
using WardenData.DTOs;

namespace WardenData.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly AppDbContext _context;
    private static readonly Dictionary<int, dynamic> _runesInfo;

    static StatisticsController()
    {
        var runesJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "runes.json");
        var runesJson = System.IO.File.ReadAllText(runesJsonPath);
        var runesData = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(runesJson);
        
        _runesInfo = new Dictionary<int, dynamic>();
        foreach (var rune in runesData!)
        {
            var runeInfo = JsonSerializer.Deserialize<JsonElement>(rune.Value.ToString()!);
            if (runeInfo.TryGetProperty("id", out JsonElement idArray))
            {
                foreach (var id in idArray.EnumerateArray())
                {
                    JsonElement paElement, raElement;
                    _runesInfo[id.GetInt32()] = new
                    {
                        Name = runeInfo.GetProperty("name").GetString(),
                        RuneName = runeInfo.GetProperty("rune_name").GetString(),
                        Weight = runeInfo.GetProperty("weight").GetDouble(),
                        RuneValue = runeInfo.GetProperty("rune").GetInt32(),
                        PaRuneValue = runeInfo.TryGetProperty("pa_rune", out paElement) && paElement.ValueKind != JsonValueKind.Null ? paElement.GetInt32() : (int?)null,
                        RaRuneValue = runeInfo.TryGetProperty("ra_rune", out raElement) && raElement.ValueKind != JsonValueKind.Null ? raElement.GetInt32() : (int?)null,
                        EffectId = runeInfo.GetProperty("effect_id").GetInt32()
                    };
                }
            }
        }
    }

    public StatisticsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("rune-success-rates")]
    public async Task<ActionResult<List<RuneSuccessStatsDto>>> GetRuneSuccessRates(
        [FromQuery] int? runeId = null,
        [FromQuery] string? effectName = null,
        [FromQuery] bool? isTenta = null)
    {
        var query = _context.RuneHistories.AsQueryable();

        if (runeId.HasValue)
            query = query.Where(r => r.RuneId == runeId.Value);
        if (isTenta.HasValue)
            query = query.Where(r => r.IsTenta == isTenta.Value);

        var results = await query
            .GroupBy(r => new { r.RuneId, r.IsTenta })
            .Select(g => new
            {
                g.Key.RuneId,
                g.Key.IsTenta,
                TotalAttempts = g.Count(),
                Successes = g.Sum(r => r.HasSucceed ? 1 : 0),
                SuccessRate = g.Average(r => r.HasSucceed ? 1.0 : 0.0) * 100
            })
            .ToListAsync();

        var statsDto = results.Select(r =>
        {
            var runeInfo = _runesInfo.TryGetValue(r.RuneId, out var info) ? info : null;
            return new RuneSuccessStatsDto
            {
                RuneId = r.RuneId,
                RuneName = runeInfo?.RuneName ?? $"Rune {r.RuneId}",
                EffectName = runeInfo?.Name ?? "Unknown",
                TotalAttempts = r.TotalAttempts,
                Successes = r.Successes,
                SuccessRate = Math.Round(r.SuccessRate, 2),
                IsTenta = r.IsTenta
            };
        }).ToList();

        if (!string.IsNullOrEmpty(effectName))
        {
            statsDto = statsDto.Where(s => s.EffectName.Contains(effectName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Ok(statsDto.OrderByDescending(s => s.SuccessRate));
    }

    [HttpGet("effect-progression")]
    public async Task<ActionResult<List<EffectProgressionDto>>> GetEffectProgression(
        [FromQuery] Guid? sessionId = null,
        [FromQuery] string? effectName = null,
        [FromQuery] int? minValue = null,
        [FromQuery] int? maxValue = null)
    {
        var query = from rh in _context.RuneHistories
                    join s in _context.Sessions on rh.SessionId equals s.Id
                    select new { rh, s };

        if (sessionId.HasValue)
            query = query.Where(x => x.s.Id == sessionId.Value);

        var results = await query.ToListAsync();

        var progressions = new List<EffectProgressionDto>();

        foreach (var result in results)
        {
            if (result.rh.EffectsAfter != null)
            {
                var effectsAfter = JsonSerializer.Deserialize<JsonElement>(result.rh.EffectsAfter);
                if (effectsAfter.TryGetProperty("effects", out var effectsArray))
                {
                    foreach (var effect in effectsArray.EnumerateArray())
                    {
                        var name = effect.GetProperty("effect_name").GetString() ?? "";
                        var currentVal = effect.GetProperty("current_value").GetInt32();
                        var desiredVal = effect.GetProperty("desired_value").GetInt32();
                        var minVal = effect.GetProperty("min_value").GetInt32();
                        var maxVal = effect.GetProperty("max_value").GetInt32();

                        if (!string.IsNullOrEmpty(effectName) && 
                            !name.Contains(effectName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (minValue.HasValue && currentVal < minValue.Value)
                            continue;
                        if (maxValue.HasValue && currentVal > maxValue.Value)
                            continue;

                        progressions.Add(new EffectProgressionDto
                        {
                            SessionId = result.s.Id,
                            RuneId = result.rh.RuneId,
                            EffectName = name,
                            CurrentValue = currentVal,
                            DesiredValue = desiredVal,
                            MinValue = minVal,
                            MaxValue = maxVal,
                            HasSucceed = result.rh.HasSucceed,
                            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(result.s.Timestamp).DateTime
                        });
                    }
                }
            }
        }

        return Ok(progressions.OrderBy(p => p.Timestamp).ThenBy(p => p.SessionId));
    }

    [HttpGet("cost-efficiency")]
    public async Task<ActionResult<List<CostEfficiencyDto>>> GetCostEfficiency(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? minCost = null,
        [FromQuery] int? maxCost = null)
    {
        var query = from s in _context.Sessions
                    join o in _context.Orders on s.OrderId equals o.Id
                    join rh in _context.RuneHistories on s.Id equals rh.SessionId into sessionRunes
                    select new { s, o, sessionRunes };

        var results = await query.ToListAsync();

        var costEfficiencies = new List<CostEfficiencyDto>();

        foreach (var result in results)
        {
            var sessionDate = DateTimeOffset.FromUnixTimeMilliseconds(result.s.Timestamp).DateTime;
            
            if (startDate.HasValue && sessionDate < startDate.Value) continue;
            if (endDate.HasValue && sessionDate > endDate.Value) continue;

            var totalRunesUsed = result.sessionRunes.Count();
            var totalSuccesses = result.sessionRunes.Count(r => r.HasSucceed);
            
            long totalCost = 0;
            if (result.s.RunesPrices != null)
            {
                var runesPrices = JsonSerializer.Deserialize<JsonElement>(result.s.RunesPrices);
                if (runesPrices.TryGetProperty("rune_prices", out var pricesObj))
                {
                    foreach (var rune in result.sessionRunes)
                    {
                        if (pricesObj.TryGetProperty(rune.RuneId.ToString(), out var priceElement))
                        {
                            totalCost += priceElement.GetInt64();
                        }
                    }
                }
            }

            if (minCost.HasValue && totalCost < minCost.Value) continue;
            if (maxCost.HasValue && totalCost > maxCost.Value) continue;

            var successRate = totalRunesUsed > 0 ? (double)totalSuccesses / totalRunesUsed * 100 : 0;
            var costPerSuccess = totalSuccesses > 0 ? (double)totalCost / totalSuccesses : 0;

            costEfficiencies.Add(new CostEfficiencyDto
            {
                SessionId = result.s.Id,
                OrderName = result.o.Name,
                TotalRunesUsed = totalRunesUsed,
                TotalSuccesses = totalSuccesses,
                TotalCost = totalCost,
                SuccessRate = Math.Round(successRate, 2),
                CostPerSuccess = Math.Round(costPerSuccess, 2),
                SessionDate = sessionDate
            });
        }

        return Ok(costEfficiencies.OrderBy(ce => ce.CostPerSuccess));
    }

    [HttpGet("tenta-comparison")]
    public async Task<ActionResult<List<TentaComparisonDto>>> GetTentaComparison(
        [FromQuery] string? effectName = null)
    {
        var results = await _context.RuneHistories.ToListAsync();

        var comparisons = new List<TentaComparisonDto>();
        var effectGroups = new Dictionary<string, Dictionary<bool, (int attempts, int successes)>>();

        foreach (var rh in results)
        {
            if (rh.EffectsAfter != null)
            {
                var effectsAfter = JsonSerializer.Deserialize<JsonElement>(rh.EffectsAfter);
                if (effectsAfter.TryGetProperty("effects", out var effectsArray))
                {
                    foreach (var effect in effectsArray.EnumerateArray())
                    {
                        var name = effect.GetProperty("effect_name").GetString() ?? "";
                        
                        if (!string.IsNullOrEmpty(effectName) && 
                            !name.Contains(effectName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!effectGroups.ContainsKey(name))
                            effectGroups[name] = new Dictionary<bool, (int, int)> { { true, (0, 0) }, { false, (0, 0) } };

                        var current = effectGroups[name][rh.IsTenta];
                        effectGroups[name][rh.IsTenta] = (current.attempts + 1, current.successes + (rh.HasSucceed ? 1 : 0));
                    }
                }
            }
        }

        foreach (var effectGroup in effectGroups)
        {
            foreach (var tentaGroup in effectGroup.Value)
            {
                var successRate = tentaGroup.Value.attempts > 0 
                    ? (double)tentaGroup.Value.successes / tentaGroup.Value.attempts * 100 
                    : 0;

                comparisons.Add(new TentaComparisonDto
                {
                    EffectName = effectGroup.Key,
                    IsTenta = tentaGroup.Key,
                    TotalAttempts = tentaGroup.Value.attempts,
                    Successes = tentaGroup.Value.successes,
                    SuccessRate = Math.Round(successRate, 2)
                });
            }
        }

        return Ok(comparisons.OrderBy(c => c.EffectName).ThenByDescending(c => c.IsTenta));
    }

    [HttpGet("session-progress/{sessionId}")]
    public async Task<ActionResult<SessionProgressDto>> GetSessionProgress(Guid sessionId)
    {
        var session = await _context.Sessions
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            return NotFound($"Session {sessionId} not found");

        var runeHistories = await _context.RuneHistories
            .Where(r => r.SessionId == sessionId)
            .OrderBy(r => r.Id)
            .ToListAsync();

        var steps = new List<ProgressStepDto>();
        JsonElement? previousEffects = null;

        if (session.InitialEffects != null)
        {
            previousEffects = JsonSerializer.Deserialize<JsonElement>(session.InitialEffects);
        }

        for (int i = 0; i < runeHistories.Count; i++)
        {
            var rh = runeHistories[i];
            var runeInfo = _runesInfo.TryGetValue(rh.RuneId, out var info) ? info : null;
            
            var step = new ProgressStepDto
            {
                StepOrder = i + 1,
                RuneId = rh.RuneId,
                RuneName = runeInfo?.RuneName ?? $"Rune {rh.RuneId}",
                IsTenta = rh.IsTenta,
                HasSucceed = rh.HasSucceed,
                EffectChanges = new List<EffectChangeDto>()
            };

            if (rh.EffectsAfter != null)
            {
                var currentEffects = JsonSerializer.Deserialize<JsonElement>(rh.EffectsAfter);
                if (currentEffects.TryGetProperty("effects", out var effectsArray) && previousEffects.HasValue)
                {
                    var prevEffectsArray = previousEffects.Value.TryGetProperty("effects", out var prevArray) ? prevArray : new JsonElement();
                    
                    foreach (var effect in effectsArray.EnumerateArray())
                    {
                        var effectName = effect.GetProperty("effect_name").GetString() ?? "";
                        var currentValue = effect.GetProperty("current_value").GetInt32();
                        var desiredValue = effect.GetProperty("desired_value").GetInt32();
                        
                        var previousValue = currentValue;
                        if (prevEffectsArray.ValueKind != JsonValueKind.Undefined)
                        {
                            foreach (var prevEffect in prevEffectsArray.EnumerateArray())
                            {
                                if (prevEffect.GetProperty("effect_name").GetString() == effectName)
                                {
                                    previousValue = prevEffect.GetProperty("current_value").GetInt32();
                                    break;
                                }
                            }
                        }

                        step.EffectChanges.Add(new EffectChangeDto
                        {
                            EffectName = effectName,
                            PreviousValue = previousValue,
                            CurrentValue = currentValue,
                            DesiredValue = desiredValue,
                            Change = currentValue - previousValue
                        });
                    }
                }
                previousEffects = currentEffects;
            }

            steps.Add(step);
        }

        var sessionDate = DateTimeOffset.FromUnixTimeMilliseconds(session.Timestamp).DateTime;
        var isCompleted = steps.Any() && steps.Last().EffectChanges.Any(ec => ec.CurrentValue >= ec.DesiredValue);

        return Ok(new SessionProgressDto
        {
            SessionId = sessionId,
            OrderName = session.Order?.Name ?? "Unknown Order",
            Steps = steps,
            SessionDate = sessionDate,
            TotalSteps = steps.Count,
            IsCompleted = isCompleted
        });
    }

    [HttpGet("rune-usage-stats")]
    public async Task<ActionResult<List<RuneUsageStatsDto>>> GetRuneUsageStats()
    {
        var usageStats = await _context.RuneHistories
            .GroupBy(r => r.RuneId)
            .Select(g => new
            {
                RuneId = g.Key,
                TotalUsage = g.Count(),
                OverallSuccessRate = g.Average(r => r.HasSucceed ? 1.0 : 0.0) * 100
            })
            .ToListAsync();

        var statsDto = usageStats.Select(s =>
        {
            var runeInfo = _runesInfo.TryGetValue(s.RuneId, out var info) ? info : null;
            return new RuneUsageStatsDto
            {
                RuneId = s.RuneId,
                RuneName = runeInfo?.RuneName ?? $"Rune {s.RuneId}",
                EffectName = runeInfo?.Name ?? "Unknown",
                Weight = runeInfo?.Weight ?? 0,
                RuneValue = runeInfo?.RuneValue ?? 0,
                PaRuneValue = runeInfo?.PaRuneValue,
                RaRuneValue = runeInfo?.RaRuneValue,
                TotalUsage = s.TotalUsage,
                OverallSuccessRate = Math.Round(s.OverallSuccessRate, 2)
            };
        }).ToList();

        return Ok(statsDto.OrderByDescending(s => s.TotalUsage));
    }
}