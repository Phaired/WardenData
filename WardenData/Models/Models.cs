using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WardenData.Models;

public enum UserRole
{
    User = 0,
    Admin = 1
}

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string Token { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public UserRole Role { get; set; } = UserRole.User;

    // Navigation properties
    public List<Order> Orders { get; set; } = new();
}

public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

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
    public int UserId { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required]
    public long Timestamp { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    public List<SessionEffect> SessionEffects { get; set; } = new();
    public List<SessionRunePrice> SessionRunePrices { get; set; } = new();
}

public class RuneHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int SessionId { get; set; }

    [Required]
    public int RuneId { get; set; }

    [Required]
    public bool IsTenta { get; set; }

    [Required]
    public bool HasSucceed { get; set; }

    [Required]
    public bool HasSynchronized { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("SessionId")]
    public Session Session { get; set; } = null!;

    public List<RuneHistoryEffect> RuneHistoryEffects { get; set; } = new();
}

public class SessionEffect
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SessionId { get; set; }

    [Required]
    public string EffectName { get; set; }

    [Required]
    public long CurrentValue { get; set; }

    [ForeignKey("SessionId")]
    public Session Session { get; set; }
}

public class SessionRunePrice
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SessionId { get; set; }

    [Required]
    public int RuneId { get; set; }

    [Required]
    public string RuneName { get; set; }

    [Required]
    public long Price { get; set; }

    [ForeignKey("SessionId")]
    public Session Session { get; set; }
}

public class RuneHistoryEffect
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int RuneHistoryId { get; set; }

    [Required]
    public string EffectName { get; set; }

    [Required]
    public long CurrentValue { get; set; }

    [ForeignKey("RuneHistoryId")]
    public RuneHistory RuneHistory { get; set; }
}