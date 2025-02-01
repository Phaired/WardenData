namespace WardenData.Models;

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public AppDbContext() { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderEffect> OrderEffects => Set<OrderEffect>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<RuneHistory> RuneHistories => Set<RuneHistory>();

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
                
            entity.Property(e => e.InitialEffects)
                .IsRequired()
                .HasColumnType("jsonb")
                .HasColumnName("initial_effects");
                
            entity.Property(e => e.RunesPrices)
                .IsRequired()
                .HasColumnType("jsonb")
                .HasColumnName("runes_prices");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.Sessions)
                .HasForeignKey(e => e.OrderId)
                .HasConstraintName("fk_sessions_orders");
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
                
            entity.Property(e => e.EffectsAfter)
                .IsRequired()
                .HasColumnType("jsonb")
                .HasColumnName("effects_after");
                
            entity.Property(e => e.HasSucceed)
                .HasColumnName("has_succeed")
                .HasDefaultValue(false);

            // Suppression de la propriété HasSynchronized côté serveur

            entity.HasOne(e => e.Session)
                .WithMany()
                .HasForeignKey(e => e.SessionId)
                .HasConstraintName("fk_rune_histories_sessions");
        });
    }
}
