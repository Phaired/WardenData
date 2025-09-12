using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WardenData.Models;

public class Effect
{
    [Key]
    public short Id { get; set; }
    
    [Required]
    public string Code { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    public string? Unit { get; set; }
    
    public bool IsPercent { get; set; } = false;
    
    public int? MinPossible { get; set; }
    
    public int? MaxPossible { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<OrderEffect> OrderEffects { get; set; } = new();
    public List<SessionInitialEffect> SessionInitialEffects { get; set; } = new();
    public List<RuneHistoryEffectChange> RuneHistoryEffectChanges { get; set; } = new();
}

public class Rune
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<SessionRunePrice> SessionRunePrices { get; set; } = new();
    public List<RuneHistory> RuneHistories { get; set; } = new();
}

public class Order
{
    [Key]
    public Guid Id { get; set; }  // UUID généré côté serveur

    [Required]
    public int OriginalId { get; set; }  // Identifiant d'origine du client

    [Required]
    public string Name { get; set; }
    
    public List<OrderEffect> OrderEffects { get; set; } = new();
    public List<Session> Sessions { get; set; } = new();
}

public class OrderEffect
{
    [Key]
    public Guid Id { get; set; }  // UUID serveur

    [Required]
    public int OriginalId { get; set; }  // Identifiant d'origine

    [Required]
    public Guid OrderId { get; set; }  // FK vers l'UUID de Order
    
    [Required]
    public short EffectId { get; set; }  // FK vers Effect
    
    // Temporary - for backward compatibility during migration
    public string? EffectName { get; set; }
    
    
    public long MinValue { get; set; }
    public long MaxValue { get; set; }
    public long DesiredValue { get; set; }

    [ForeignKey("OrderId")]
    public Order Order { get; set; }
    
    [ForeignKey("EffectId")]
    public Effect Effect { get; set; }
}

public class Session
{
    [Key]
    public Guid Id { get; set; }  // UUID serveur

    [Required]
    public int OriginalId { get; set; }  // Identifiant d'origine

    [Required]
    public Guid OrderId { get; set; }  // FK vers l'UUID de Order

    [Required]
    public long Timestamp { get; set; }
    
    public DateTime? StartedAt { get; set; }
    
    // Temporary - for backward compatibility during migration
    public string? InitialEffects { get; set; }
    public string? RunesPrices { get; set; }

    [ForeignKey("OrderId")]
    public Order Order { get; set; }
    
    public List<SessionInitialEffect> SessionInitialEffects { get; set; } = new();
    public List<SessionRunePrice> SessionRunePrices { get; set; } = new();
    public List<RuneHistory> RuneHistories { get; set; } = new();
}

public class RuneHistory
{
    [Key]
    public Guid Id { get; set; }  // UUID serveur

    [Required]
    public int OriginalId { get; set; }  // Identifiant d'origine

    [Required]
    public Guid SessionId { get; set; }  // FK vers l'UUID de Session

    [Required]
    public int RuneId { get; set; }  // FK vers Rune
    
    [Required]
    public bool IsTenta { get; set; }
    
    [Required]
    public bool HasSucceed { get; set; }
    
    public DateTime? AppliedAt { get; set; }
    
    // Temporary - for backward compatibility during migration  
    public string? EffectsAfter { get; set; }

    [ForeignKey("SessionId")]
    public Session Session { get; set; }
    
    [ForeignKey("RuneId")]
    public Rune Rune { get; set; }
    
    public List<RuneHistoryEffectChange> RuneHistoryEffectChanges { get; set; } = new();
}

public class SessionInitialEffect
{
    [Required]
    public Guid SessionId { get; set; }
    
    [Required]
    public short EffectId { get; set; }
    
    [Required]
    public int Value { get; set; }
    
    [ForeignKey("SessionId")]
    public Session Session { get; set; }
    
    [ForeignKey("EffectId")]
    public Effect Effect { get; set; }
}

public class SessionRunePrice
{
    [Required]
    public Guid SessionId { get; set; }
    
    [Required]
    public int RuneId { get; set; }
    
    [Required]
    public int Price { get; set; }
    
    [ForeignKey("SessionId")]
    public Session Session { get; set; }
    
    [ForeignKey("RuneId")]
    public Rune Rune { get; set; }
}

public class RuneHistoryEffectChange
{
    [Required]
    public Guid RuneHistoryId { get; set; }
    
    [Required]
    public short EffectId { get; set; }
    
    public int? OldValue { get; set; }  // NULL for first application
    
    [Required]
    public int NewValue { get; set; }
    
    [ForeignKey("RuneHistoryId")]
    public RuneHistory RuneHistory { get; set; }
    
    [ForeignKey("EffectId")]
    public Effect Effect { get; set; }
}
