namespace WardenData.Models;

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public AppDbContext() { }

    public DbSet<Effect> Effects => Set<Effect>();
    public DbSet<Rune> Runes => Set<Rune>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderEffect> OrderEffects => Set<OrderEffect>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionInitialEffect> SessionInitialEffects => Set<SessionInitialEffect>();
    public DbSet<SessionRunePrice> SessionRunePrices => Set<SessionRunePrice>();
    public DbSet<RuneHistory> RuneHistories => Set<RuneHistory>();
    public DbSet<RuneHistoryEffectChange> RuneHistoryEffectChanges => Set<RuneHistoryEffectChange>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Exemple de connexion à PostgreSQL
            optionsBuilder.UseNpgsql("Host=localhost;Database=warden;Username=adm;Password=adm");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuration de l'entité Effect
        modelBuilder.Entity<Effect>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("smallint");
            
            entity.Property(e => e.Code)
                .IsRequired()
                .HasColumnName("code");
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasColumnName("name");
            
            entity.Property(e => e.Unit)
                .HasColumnName("unit");
            
            entity.Property(e => e.IsPercent)
                .HasColumnName("is_percent")
                .HasDefaultValue(false);
            
            entity.Property(e => e.MinPossible)
                .HasColumnName("min_possible");
            
            entity.Property(e => e.MaxPossible)
                .HasColumnName("max_possible");
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.Code)
                .IsUnique()
                .HasDatabaseName("ix_effects_code");
            
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("ix_effects_name");
        });

        // Configuration de l'entité Rune
        modelBuilder.Entity<Rune>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id");
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasColumnName("name");
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
        });

        // Configuration de l'entité Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");
            
            // Mapping de l'identifiant d'origine
            entity.Property(e => e.OriginalId)
                .IsRequired()
                .HasColumnName("original_id");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("name");
            
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("ix_orders_name");
        });

        // Configuration de l'entité OrderEffect
        modelBuilder.Entity<OrderEffect>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");

            entity.Property(e => e.OriginalId)
                .IsRequired()
                .HasColumnName("original_id");

            entity.Property(e => e.OrderId)
                .IsRequired()
                .HasColumnName("order_id")
                .HasColumnType("uuid");
                
            entity.Property(e => e.EffectId)
                .IsRequired()
                .HasColumnName("effect_id")
                .HasColumnType("smallint");
                
                
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
            
            entity.HasOne(e => e.Effect)
                .WithMany(ef => ef.OrderEffects)
                .HasForeignKey(e => e.EffectId)
                .HasConstraintName("fk_order_effects_effects");
        });

        // Configuration de l'entité Session
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");

            entity.Property(e => e.OriginalId)
                .IsRequired()
                .HasColumnName("original_id");

            entity.Property(e => e.OrderId)
                .IsRequired()
                .HasColumnName("order_id")
                .HasColumnType("uuid");
                
            entity.Property(e => e.Timestamp)
                .HasColumnName("timestamp");
                
            entity.Property(e => e.StartedAt)
                .HasColumnName("started_at");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.Sessions)
                .HasForeignKey(e => e.OrderId)
                .HasConstraintName("fk_sessions_orders");
            
            entity.HasIndex(e => e.StartedAt)
                .HasDatabaseName("ix_sessions_started_at");
        });

        // Configuration de l'entité RuneHistory
        modelBuilder.Entity<RuneHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");

            entity.Property(e => e.OriginalId)
                .IsRequired()
                .HasColumnName("original_id");

            entity.Property(e => e.SessionId)
                .IsRequired()
                .HasColumnName("session_id")
                .HasColumnType("uuid");
                
            entity.Property(e => e.RuneId)
                .HasColumnName("rune_id");
                
            entity.Property(e => e.IsTenta)
                .HasColumnName("is_tenta");
                
            entity.Property(e => e.HasSucceed)
                .HasColumnName("has_succeed")
                .HasDefaultValue(false);
                
            entity.Property(e => e.AppliedAt)
                .HasColumnName("applied_at");

            entity.HasOne(e => e.Session)
                .WithMany(s => s.RuneHistories)
                .HasForeignKey(e => e.SessionId)
                .HasConstraintName("fk_rune_histories_sessions");
            
            entity.HasOne(e => e.Rune)
                .WithMany(r => r.RuneHistories)
                .HasForeignKey(e => e.RuneId)
                .HasConstraintName("fk_rune_histories_runes");
            
            entity.HasIndex(e => new { e.SessionId, e.OriginalId })
                .IsUnique()
                .HasDatabaseName("uq_rh_session_step");
                
            entity.HasIndex(e => e.AppliedAt)
                .HasDatabaseName("ix_rune_histories_applied_at");
        });

        // Configuration de l'entité SessionInitialEffect
        modelBuilder.Entity<SessionInitialEffect>(entity =>
        {
            entity.HasKey(e => new { e.SessionId, e.EffectId });
            
            entity.Property(e => e.SessionId)
                .HasColumnName("session_id")
                .HasColumnType("uuid");
            
            entity.Property(e => e.EffectId)
                .HasColumnName("effect_id")
                .HasColumnType("smallint");
            
            entity.Property(e => e.Value)
                .HasColumnName("value");

            entity.HasOne(e => e.Session)
                .WithMany(s => s.SessionInitialEffects)
                .HasForeignKey(e => e.SessionId)
                .HasConstraintName("fk_session_initial_effects_sessions");
            
            entity.HasOne(e => e.Effect)
                .WithMany(ef => ef.SessionInitialEffects)
                .HasForeignKey(e => e.EffectId)
                .HasConstraintName("fk_session_initial_effects_effects");
                
            entity.HasIndex(e => e.EffectId)
                .HasDatabaseName("ix_sie_effect");
        });

        // Configuration de l'entité SessionRunePrice
        modelBuilder.Entity<SessionRunePrice>(entity =>
        {
            entity.HasKey(e => new { e.SessionId, e.RuneId });
            
            entity.Property(e => e.SessionId)
                .HasColumnName("session_id")
                .HasColumnType("uuid");
            
            entity.Property(e => e.RuneId)
                .HasColumnName("rune_id");
            
            entity.Property(e => e.Price)
                .HasColumnName("price");

            entity.HasOne(e => e.Session)
                .WithMany(s => s.SessionRunePrices)
                .HasForeignKey(e => e.SessionId)
                .HasConstraintName("fk_session_rune_prices_sessions");
            
            entity.HasOne(e => e.Rune)
                .WithMany(r => r.SessionRunePrices)
                .HasForeignKey(e => e.RuneId)
                .HasConstraintName("fk_session_rune_prices_runes");
                
            entity.HasIndex(e => e.RuneId)
                .HasDatabaseName("ix_srp_rune");
        });

        // Configuration de l'entité RuneHistoryEffectChange
        modelBuilder.Entity<RuneHistoryEffectChange>(entity =>
        {
            entity.HasKey(e => new { e.RuneHistoryId, e.EffectId });
            
            entity.Property(e => e.RuneHistoryId)
                .HasColumnName("rune_history_id")
                .HasColumnType("uuid");
            
            entity.Property(e => e.EffectId)
                .HasColumnName("effect_id")
                .HasColumnType("smallint");
            
            entity.Property(e => e.OldValue)
                .HasColumnName("old_value");
            
            entity.Property(e => e.NewValue)
                .HasColumnName("new_value");

            entity.HasOne(e => e.RuneHistory)
                .WithMany(rh => rh.RuneHistoryEffectChanges)
                .HasForeignKey(e => e.RuneHistoryId)
                .HasConstraintName("fk_rune_history_effect_changes_rune_histories");
            
            entity.HasOne(e => e.Effect)
                .WithMany(ef => ef.RuneHistoryEffectChanges)
                .HasForeignKey(e => e.EffectId)
                .HasConstraintName("fk_rune_history_effect_changes_effects");
                
            entity.HasIndex(e => new { e.EffectId, e.RuneHistoryId })
                .HasDatabaseName("ix_rhec_effect");
        });
    }
}
