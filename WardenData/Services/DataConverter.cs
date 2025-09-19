using System.Text.Json;
using EFCore.BulkExtensions;
using WardenData.Controllers;
using WardenData.Models;

namespace WardenData.Services;

public class DataConverter : IDataConverter
{
    private readonly ILogger<DataConverter> _logger;

    public DataConverter(ILogger<DataConverter> logger)
    {
        _logger = logger;
    }

    public async Task ProcessSessionDataAsync(string jsonData, AppDbContext context)
    {
        try
        {
            var sessionDtos = JsonSerializer.Deserialize<List<SessionDTO>>(jsonData);
            if (sessionDtos == null || !sessionDtos.Any())
            {
                _logger.LogWarning("No session data to process");
                return;
            }

            var sessions = new List<Session>();
            var sessionEffects = new List<SessionEffect>();
            var sessionRunePrices = new List<SessionRunePrice>();

            foreach (var dto in sessionDtos)
            {
                // Convert main session
                var session = new Session
                {
                    Id = dto.Id,
                    OrderId = dto.OrderId,
                    Timestamp = dto.Timestamp
                };
                sessions.Add(session);

                // Parse and convert initial effects
                var initialEffects = JsonSerializer.Deserialize<List<EffectData>>(dto.InitialEffects);
                if (initialEffects != null)
                {
                    foreach (var effect in initialEffects)
                    {
                        sessionEffects.Add(new SessionEffect
                        {
                            SessionId = dto.Id,
                            EffectName = effect.effect_name,
                            CurrentValue = effect.current_value
                        });
                    }
                }

                // Parse and convert rune prices
                var runesPrices = JsonSerializer.Deserialize<List<RunePriceData>>(dto.RunesPrices);
                if (runesPrices != null)
                {
                    foreach (var rune in runesPrices)
                    {
                        sessionRunePrices.Add(new SessionRunePrice
                        {
                            SessionId = dto.Id,
                            RuneId = rune.id,
                            RuneName = rune.name,
                            Price = rune.price
                        });
                    }
                }
            }

            // Bulk insert all data
            await context.BulkInsertOrUpdateAsync(sessions);
            await context.BulkInsertOrUpdateAsync(sessionEffects);
            await context.BulkInsertOrUpdateAsync(sessionRunePrices);

            _logger.LogInformation("Processed {SessionCount} sessions with {EffectCount} effects and {PriceCount} rune prices",
                sessions.Count, sessionEffects.Count, sessionRunePrices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing session data");
            throw;
        }
    }

    public async Task ProcessOrderDataAsync(string jsonData, AppDbContext context)
    {
        try
        {
            var orderDtos = JsonSerializer.Deserialize<List<OrderDTO>>(jsonData);
            if (orderDtos == null || !orderDtos.Any())
            {
                _logger.LogWarning("No order data to process");
                return;
            }

            var orders = orderDtos.Select(dto => new Order
            {
                Id = dto.Id,
                Name = dto.Name
            }).ToList();

            await context.BulkInsertOrUpdateAsync(orders);

            _logger.LogInformation("Processed {OrderCount} orders", orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order data");
            throw;
        }
    }

    public async Task ProcessOrderEffectDataAsync(string jsonData, AppDbContext context)
    {
        try
        {
            var effectDtos = JsonSerializer.Deserialize<List<OrderEffectDTO>>(jsonData);
            if (effectDtos == null || !effectDtos.Any())
            {
                _logger.LogWarning("No order effect data to process");
                return;
            }

            var effects = effectDtos.Select(dto => new OrderEffect
            {
                Id = dto.Id,
                OrderId = dto.OrderId,
                EffectName = dto.EffectName,
                MinValue = dto.MinValue,
                MaxValue = dto.MaxValue,
                DesiredValue = dto.DesiredValue
            }).ToList();

            await context.BulkInsertOrUpdateAsync(effects);

            _logger.LogInformation("Processed {EffectCount} order effects", effects.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order effect data");
            throw;
        }
    }

    public async Task ProcessRuneHistoryDataAsync(string jsonData, AppDbContext context)
    {
        try
        {
            var historyDtos = JsonSerializer.Deserialize<List<RuneHistoryDTO>>(jsonData);
            if (historyDtos == null || !historyDtos.Any())
            {
                _logger.LogWarning("No rune history data to process");
                return;
            }

            var histories = new List<RuneHistory>();
            var historyEffects = new List<RuneHistoryEffect>();

            foreach (var dto in historyDtos)
            {
                // Convert main rune history
                var history = new RuneHistory
                {
                    Id = dto.Id,
                    SessionId = dto.SessionId,
                    RuneId = dto.RuneId,
                    IsTenta = dto.IsTenta,
                    HasSucceed = dto.HasSucceed,
                    HasSynchronized = false // Default value
                };
                histories.Add(history);

                // Parse and convert effects after
                var effectsAfterJson = dto.EffectsAfter.ToString();
                var effectsAfter = JsonSerializer.Deserialize<List<EffectData>>(effectsAfterJson);
                if (effectsAfter != null)
                {
                    foreach (var effect in effectsAfter)
                    {
                        historyEffects.Add(new RuneHistoryEffect
                        {
                            RuneHistoryId = dto.Id,
                            EffectName = effect.effect_name,
                            CurrentValue = effect.current_value
                        });
                    }
                }
            }

            // Bulk insert all data
            await context.BulkInsertOrUpdateAsync(histories);
            await context.BulkInsertOrUpdateAsync(historyEffects);

            _logger.LogInformation("Processed {HistoryCount} rune histories with {EffectCount} effects",
                histories.Count, historyEffects.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing rune history data");
            throw;
        }
    }
}

// Helper classes for JSON parsing
public class EffectData
{
    public string effect_name { get; set; } = null!;
    public long current_value { get; set; }
}

public class RunePriceData
{
    public int id { get; set; }
    public string name { get; set; } = null!;
    public long price { get; set; }
}