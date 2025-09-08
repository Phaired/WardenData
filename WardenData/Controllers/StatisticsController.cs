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

    private int GetExpectedGainForRune(int runeId, bool isTenta)
    {
        if (_runesInfo.TryGetValue(runeId, out var runeInfo))
        {
            // For tenta runes, use pa_rune or ra_rune value if available
            if (isTenta)
            {
                return runeInfo.PaRuneValue ?? runeInfo.RaRuneValue ?? runeInfo.RuneValue;
            }
            // For normal runes, use the base rune value
            return runeInfo.RuneValue;
        }
        // Fallback to minimum expected gain if rune info not found
        return 1;
    }

    private int CalculateRealSuccessesForSession(Session session, List<RuneHistory> runeHistories)
    {
        var successCount = 0;
        
        // Start with initial effects as the "previous" state
        JsonElement? previousEffectsJson = null;
        if (session.InitialEffects != null)
        {
            try
            {
                previousEffectsJson = JsonSerializer.Deserialize<JsonElement>(session.InitialEffects);
            }
            catch (JsonException)
            {
                return 0; // Skip session with malformed initial effects
            }
        }

        var orderedHistories = runeHistories.OrderBy(x => x.Id).ToList();

        foreach (var rh in orderedHistories)
        {
            if (rh.EffectsAfter != null && previousEffectsJson.HasValue)
            {
                try
                {
                    var effectsAfter = JsonSerializer.Deserialize<JsonElement>(rh.EffectsAfter);
                    
                    if (effectsAfter.TryGetProperty("effects", out var effectsAfterArray) &&
                        previousEffectsJson.Value.TryGetProperty("effects", out var previousEffectsArray))
                    {
                        // Create dictionary of previous values for quick lookup
                        var previousValues = new Dictionary<string, int>();
                        foreach (var prevEffect in previousEffectsArray.EnumerateArray())
                        {
                            if (prevEffect.TryGetProperty("effect_name", out var nameEl) &&
                                prevEffect.TryGetProperty("current_value", out var valueEl))
                            {
                                previousValues[nameEl.GetString() ?? ""] = valueEl.GetInt32();
                            }
                        }

                        bool hasAnySuccess = false;
                        foreach (var effect in effectsAfterArray.EnumerateArray())
                        {
                            if (effect.TryGetProperty("effect_name", out var effectNameElement))
                            {
                                var name = effectNameElement.GetString() ?? "";

                                // Get current value and previous value for this specific rune application
                                var currentValue = effect.TryGetProperty("current_value", out var currentEl) ? currentEl.GetInt32() : 0;
                                var previousValue = previousValues.TryGetValue(name, out var prevVal) ? prevVal : currentValue;
                                var actualGain = currentValue - previousValue;
                                
                                // Determine expected gain based on rune type
                                var expectedGain = GetExpectedGainForRune(rh.RuneId, rh.IsTenta);
                                
                                // Success is determined by actual gain being at least the expected gain
                                if (actualGain >= expectedGain)
                                {
                                    hasAnySuccess = true;
                                    break; // One successful effect is enough for this rune to be considered successful
                                }
                            }
                        }
                        
                        if (hasAnySuccess)
                        {
                            successCount++;
                        }
                    }
                    
                    // Update previous state for next iteration
                    previousEffectsJson = effectsAfter;
                }
                catch (JsonException)
                {
                    // Skip malformed JSON entries
                    continue;
                }
            }
        }

        return successCount;
    }

    [HttpGet("rune-success-rates")]
    public async Task<ActionResult<List<RuneSuccessStatsDto>>> GetRuneSuccessRates(
        [FromQuery] int? runeId = null,
        [FromQuery] string? effectName = null,
        [FromQuery] bool? isTenta = null,
        [FromQuery] int? minGain = 5)
    {
        // Get rune histories ordered by session and ID to properly track before/after values
        var query = from rh in _context.RuneHistories
                    join s in _context.Sessions on rh.SessionId equals s.Id
                    orderby s.Id, rh.Id
                    select new { rh, s };

        if (runeId.HasValue)
            query = query.Where(x => x.rh.RuneId == runeId.Value);
        if (isTenta.HasValue)
            query = query.Where(x => x.rh.IsTenta == isTenta.Value);

        var allHistories = await query.ToListAsync();
        
        var filteredHistories = new List<(RuneHistory history, Session session, string effectName, bool hasSucceed, int gainValue)>();

        // Group by session to properly track progression
        var sessionGroups = allHistories.GroupBy(x => x.s.Id);

        foreach (var sessionGroup in sessionGroups)
        {
            var sessionHistories = sessionGroup.OrderBy(x => x.rh.Id).ToList();
            var session = sessionGroup.First().s;
            
            // Start with initial effects as the "previous" state
            JsonElement? previousEffectsJson = null;
            if (session.InitialEffects != null)
            {
                try
                {
                    previousEffectsJson = JsonSerializer.Deserialize<JsonElement>(session.InitialEffects);
                }
                catch (JsonException)
                {
                    continue; // Skip session with malformed initial effects
                }
            }

            foreach (var item in sessionHistories)
            {
                var rh = item.rh;
                
                if (rh.EffectsAfter != null && previousEffectsJson.HasValue)
                {
                    try
                    {
                        var effectsAfter = JsonSerializer.Deserialize<JsonElement>(rh.EffectsAfter);
                        
                        if (effectsAfter.TryGetProperty("effects", out var effectsAfterArray) &&
                            previousEffectsJson.Value.TryGetProperty("effects", out var previousEffectsArray))
                        {
                            // Create dictionary of previous values for quick lookup
                            var previousValues = new Dictionary<string, int>();
                            foreach (var prevEffect in previousEffectsArray.EnumerateArray())
                            {
                                if (prevEffect.TryGetProperty("effect_name", out var nameEl) &&
                                    prevEffect.TryGetProperty("current_value", out var valueEl))
                                {
                                    previousValues[nameEl.GetString() ?? ""] = valueEl.GetInt32();
                                }
                            }

                            foreach (var effect in effectsAfterArray.EnumerateArray())
                            {
                                if (effect.TryGetProperty("effect_name", out var effectNameElement))
                                {
                                    var name = effectNameElement.GetString() ?? "";
                                    
                                    // Skip if we're filtering by effect name and this doesn't match
                                    if (!string.IsNullOrEmpty(effectName) && 
                                        !name.Contains(effectName, StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    // Get current value and previous value for this specific rune application
                                    var currentValue = effect.TryGetProperty("current_value", out var currentEl) ? currentEl.GetInt32() : 0;
                                    var previousValue = previousValues.TryGetValue(name, out var prevVal) ? prevVal : currentValue;
                                    var actualGain = currentValue - previousValue;
                                    
                                    // Determine expected gain based on rune type
                                    var expectedGain = GetExpectedGainForRune(rh.RuneId, rh.IsTenta);
                                    
                                    // Success is determined by actual gain being exactly the expected gain (runes give exact values)
                                    // For runes, success means we got at least the expected minimum gain
                                    var realSuccess = actualGain >= expectedGain;
                                    
                                    filteredHistories.Add((rh, session, name, realSuccess, actualGain));
                                }
                            }
                        }
                        
                        // Update previous state for next iteration
                        previousEffectsJson = effectsAfter;
                    }
                    catch (JsonException)
                    {
                        // Skip malformed JSON entries
                        continue;
                    }
                }
            }
        }

        // Group by RuneId, IsTenta, and EffectName for accurate statistics
        var results = filteredHistories
            .GroupBy(x => new { x.history.RuneId, x.history.IsTenta, x.effectName })
            .Select(g => new
            {
                g.Key.RuneId,
                g.Key.IsTenta,
                EffectName = g.Key.effectName,
                TotalAttempts = g.Count(),
                Successes = g.Count(x => x.hasSucceed),
                SuccessRate = g.Count() > 0 ? (double)g.Count(x => x.hasSucceed) / g.Count() * 100 : 0,
                AverageGain = g.Count() > 0 ? g.Average(x => x.gainValue) : 0
            })
            .ToList();

        var statsDto = results.Select(r =>
        {
            var runeInfo = _runesInfo.TryGetValue(r.RuneId, out var info) ? info : null;
            return new RuneSuccessStatsDto
            {
                RuneId = r.RuneId,
                RuneName = runeInfo?.RuneName ?? $"Rune {r.RuneId}",
                EffectName = r.EffectName,
                TotalAttempts = r.TotalAttempts,
                Successes = r.Successes,
                SuccessRate = Math.Round(r.SuccessRate, 2),
                IsTenta = r.IsTenta,
                AverageGain = Math.Round(r.AverageGain, 2)
            };
        }).ToList();

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
            
            // Calculate real successes using the same logic as rune-success-rates
            var realSuccesses = CalculateRealSuccessesForSession(result.s, result.sessionRunes.ToList());
            
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

            var successRate = totalRunesUsed > 0 ? (double)realSuccesses / totalRunesUsed * 100 : 0;
            var costPerSuccess = realSuccesses > 0 ? (double)totalCost / realSuccesses : 0;

            costEfficiencies.Add(new CostEfficiencyDto
            {
                SessionId = result.s.Id,
                OrderName = result.o.Name,
                TotalRunesUsed = totalRunesUsed,
                TotalSuccesses = realSuccesses,
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
        // Use the same logic as rune-success-rates to get accurate success calculations
        var query = from rh in _context.RuneHistories
                    join s in _context.Sessions on rh.SessionId equals s.Id
                    orderby s.Id, rh.Id
                    select new { rh, s };

        var allHistories = await query.ToListAsync();
        
        var effectGroups = new Dictionary<string, Dictionary<bool, (int attempts, int successes)>>();
        var sessionGroups = allHistories.GroupBy(x => x.s.Id);

        foreach (var sessionGroup in sessionGroups)
        {
            var sessionHistories = sessionGroup.OrderBy(x => x.rh.Id).ToList();
            var session = sessionGroup.First().s;
            
            // Start with initial effects as the "previous" state
            JsonElement? previousEffectsJson = null;
            if (session.InitialEffects != null)
            {
                try
                {
                    previousEffectsJson = JsonSerializer.Deserialize<JsonElement>(session.InitialEffects);
                }
                catch (JsonException)
                {
                    continue; // Skip session with malformed initial effects
                }
            }

            foreach (var item in sessionHistories)
            {
                var rh = item.rh;
                
                if (rh.EffectsAfter != null && previousEffectsJson.HasValue)
                {
                    try
                    {
                        var effectsAfter = JsonSerializer.Deserialize<JsonElement>(rh.EffectsAfter);
                        
                        if (effectsAfter.TryGetProperty("effects", out var effectsAfterArray) &&
                            previousEffectsJson.Value.TryGetProperty("effects", out var previousEffectsArray))
                        {
                            // Create dictionary of previous values for quick lookup
                            var previousValues = new Dictionary<string, int>();
                            foreach (var prevEffect in previousEffectsArray.EnumerateArray())
                            {
                                if (prevEffect.TryGetProperty("effect_name", out var nameEl) &&
                                    prevEffect.TryGetProperty("current_value", out var valueEl))
                                {
                                    previousValues[nameEl.GetString() ?? ""] = valueEl.GetInt32();
                                }
                            }

                            bool hasAnySuccess = false;
                            foreach (var effect in effectsAfterArray.EnumerateArray())
                            {
                                if (effect.TryGetProperty("effect_name", out var effectNameElement))
                                {
                                    var name = effectNameElement.GetString() ?? "";
                                    
                                    // Skip if we're filtering by effect name and this doesn't match
                                    if (!string.IsNullOrEmpty(effectName) && 
                                        !name.Contains(effectName, StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    // Initialize effect group if needed
                                    if (!effectGroups.ContainsKey(name))
                                        effectGroups[name] = new Dictionary<bool, (int, int)> { { true, (0, 0) }, { false, (0, 0) } };

                                    // Get current value and previous value for this specific rune application
                                    var currentValue = effect.TryGetProperty("current_value", out var currentEl) ? currentEl.GetInt32() : 0;
                                    var previousValue = previousValues.TryGetValue(name, out var prevVal) ? prevVal : currentValue;
                                    var actualGain = currentValue - previousValue;
                                    
                                    // Determine expected gain based on rune type
                                    var expectedGain = GetExpectedGainForRune(rh.RuneId, rh.IsTenta);
                                    
                                    // Success is determined by actual gain being at least the expected gain
                                    if (actualGain >= expectedGain)
                                    {
                                        hasAnySuccess = true;
                                    }
                                }
                            }
                            
                            // Count attempts and successes for each effect that this rune could affect
                            foreach (var effect in effectsAfterArray.EnumerateArray())
                            {
                                if (effect.TryGetProperty("effect_name", out var effectNameElement))
                                {
                                    var name = effectNameElement.GetString() ?? "";
                                    
                                    if (!string.IsNullOrEmpty(effectName) && 
                                        !name.Contains(effectName, StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    if (!effectGroups.ContainsKey(name))
                                        effectGroups[name] = new Dictionary<bool, (int, int)> { { true, (0, 0) }, { false, (0, 0) } };

                                    var current = effectGroups[name][rh.IsTenta];
                                    effectGroups[name][rh.IsTenta] = (current.attempts + 1, current.successes + (hasAnySuccess ? 1 : 0));
                                    break; // Only count once per rune
                                }
                            }
                        }
                        
                        // Update previous state for next iteration
                        previousEffectsJson = effectsAfter;
                    }
                    catch (JsonException)
                    {
                        // Skip malformed JSON entries
                        continue;
                    }
                }
            }
        }

        var comparisons = new List<TentaComparisonDto>();
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