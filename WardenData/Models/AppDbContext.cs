namespace WardenData.Models;

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public AppDbContext() { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderEffect> OrderEffects => Set<OrderEffect>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<RuneHistory> RuneHistories => Set<RuneHistory>();
    public DbSet<SessionEffect> SessionEffects => Set<SessionEffect>();
    public DbSet<SessionRunePrice> SessionRunePrices => Set<SessionRunePrice>();
    public DbSet<RuneHistoryEffect> RuneHistoryEffects => Set<RuneHistoryEffect>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=warden;Username=adm;Password=adm");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("username");

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("token");

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("password_hash");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasDefaultValue(UserRole.User);

            entity.HasIndex(e => e.Username)
                .IsUnique()
                .HasDatabaseName("ix_users_username");

            entity.HasIndex(e => e.Token)
                .IsUnique()
                .HasDatabaseName("ix_users_token");
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("name");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(e => e.UserId)
                .HasConstraintName("fk_orders_users");

            entity.HasIndex(e => new { e.UserId, e.Name })
                .HasDatabaseName("ix_orders_user_name");
        });

        // OrderEffect configuration
        modelBuilder.Entity<OrderEffect>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.OrderId)
                .HasColumnName("order_id");
                
            entity.Property(e => e.EffectName)
                .IsRequired()
                .HasColumnName("effect_name");
                
            entity.Property(e => e.MinValue)
                .HasColumnName("min_value");
                
            entity.Property(e => e.MaxValue)
                .HasColumnName("max_value");
                
            entity.Property(e => e.DesiredValue)
                .HasColumnName("desired_value");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderEffects)
                .HasForeignKey(e => e.OrderId)
                .HasConstraintName("fk_order_effects_orders");
        });

        // Session configuration
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.OrderId)
                .HasColumnName("order_id");

            entity.Property(e => e.Timestamp)
                .HasColumnName("timestamp");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .HasConstraintName("fk_sessions_users");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.Sessions)
                .HasForeignKey(e => e.OrderId)
                .HasConstraintName("fk_sessions_orders");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_sessions_user_id");
        });

        // SessionEffect configuration
        modelBuilder.Entity<SessionEffect>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SessionId)
                .HasColumnName("session_id");

            entity.Property(e => e.EffectName)
                .IsRequired()
                .HasColumnName("effect_name");

            entity.Property(e => e.CurrentValue)
                .HasColumnName("current_value");

            entity.HasOne(e => e.Session)
                .WithMany(s => s.SessionEffects)
                .HasForeignKey(e => e.SessionId)
                .HasConstraintName("fk_session_effects_sessions");
        });

        // SessionRunePrice configuration
        modelBuilder.Entity<SessionRunePrice>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SessionId)
                .HasColumnName("session_id");

            entity.Property(e => e.RuneId)
                .HasColumnName("rune_id");

            entity.Property(e => e.RuneName)
                .IsRequired()
                .HasColumnName("rune_name");

            entity.Property(e => e.Price)
                .HasColumnName("price");

            entity.HasOne(e => e.Session)
                .WithMany(s => s.SessionRunePrices)
                .HasForeignKey(e => e.SessionId)
                .HasConstraintName("fk_session_rune_prices_sessions");
        });

        // RuneHistory configuration
        modelBuilder.Entity<RuneHistory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.SessionId)
                .HasColumnName("session_id");

            entity.Property(e => e.RuneId)
                .HasColumnName("rune_id");

            entity.Property(e => e.IsTenta)
                .HasColumnName("is_tenta");

            entity.Property(e => e.HasSucceed)
                .HasColumnName("has_succeed")
                .HasDefaultValue(false);

            entity.Property(e => e.HasSynchronized)
                .HasColumnName("has_synchronized")
                .HasDefaultValue(false);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .HasConstraintName("fk_rune_histories_users");

            entity.HasOne(e => e.Session)
                .WithMany()
                .HasForeignKey(e => e.SessionId)
                .HasConstraintName("fk_rune_histories_sessions");
        });

        // RuneHistoryEffect configuration
        modelBuilder.Entity<RuneHistoryEffect>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RuneHistoryId)
                .HasColumnName("rune_history_id");

            entity.Property(e => e.EffectName)
                .IsRequired()
                .HasColumnName("effect_name");

            entity.Property(e => e.CurrentValue)
                .HasColumnName("current_value");

            entity.HasOne(e => e.RuneHistory)
                .WithMany(r => r.RuneHistoryEffects)
                .HasForeignKey(e => e.RuneHistoryId)
                .HasConstraintName("fk_rune_history_effects_rune_histories");
        });
    }
}