using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WardenData.Models;

public class Order
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<OrderEffect> OrderEffects { get; set; } = new();
    public List<Session> Sessions { get; set; } = new();
}

public class OrderEffect
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public string EffectName { get; set; }
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public int DesiredValue { get; set; }
}

public class Session
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public long Timestamp { get; set; }
    public string InitialEffects { get; set; }
    public string RunesPrices { get; set; }
    public List<RuneHistory> RuneHistories { get; set; } = new();
}

public class RuneHistory
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public Session Session { get; set; }
    public int RuneId { get; set; }
    public bool IsTenta { get; set; }
    public string EffectsAfter { get; set; }
    public bool HasSucceed { get; set; }
}