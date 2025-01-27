using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WardenData.Models;

public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    public List<OrderEffect> OrderEffects { get; set; } = new();
    public List<Session> Sessions { get; set; } = new();
}

public class OrderEffect
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int OrderId { get; set; }
    
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
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int OrderId { get; set; }
    
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
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int SessionId { get; set; }
    
    [Required]
    public int RuneId { get; set; }
    
    [Required]
    public bool IsTenta { get; set; }
    
    [Required]
    public string EffectsAfter { get; set; }
    
    [Required]
    public bool HasSucceed { get; set; }
    
    [Required]
    public bool HasSynchronized { get; set; }

    [ForeignKey("SessionId")]
    public Session Session { get; set; }
}