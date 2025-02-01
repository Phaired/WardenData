using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WardenData.Models;

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
    public string EffectName { get; set; }
    
    public long MinValue { get; set; }
    public long MaxValue { get; set; }
    public long DesiredValue { get; set; }

    [ForeignKey("OrderId")]
    public Order Order { get; set; }
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
    
    [Required]
    public string InitialEffects { get; set; }
    
    [Required]
    public string RunesPrices { get; set; }

    [ForeignKey("OrderId")]
    public Order Order { get; set; }
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
    public int RuneId { get; set; }
    
    [Required]
    public bool IsTenta { get; set; }
    
    [Required]
    public string EffectsAfter { get; set; }
    
    [Required]
    public bool HasSucceed { get; set; }

    [ForeignKey("SessionId")]
    public Session Session { get; set; }
}
