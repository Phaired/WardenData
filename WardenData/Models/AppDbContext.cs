namespace WardenData.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
        
    }
    
    public AppDbContext() { }


    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderEffect> OrderEffects => Set<OrderEffect>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<RuneHistory> RuneHistories => Set<RuneHistory>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Fallback configuration for migrations
            optionsBuilder.UseNpgsql("Host=localhost;Database=warden;Username=adm;Password=adm");
        }
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Configure Orders
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.HasIndex(e => e.Name)
                .IsUnique();
        });

        // Configure OrderEffects
        modelBuilder.Entity<OrderEffect>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderEffects)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Sessions
        modelBuilder.Entity<Session>(entity =>
        {
            entity.Property(e => e.InitialEffects)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<JsonElement>(v, jsonOptions).ToString());

            entity.Property(e => e.RunesPrices)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<JsonElement>(v, jsonOptions).ToString());
        });

        // Configure RuneHistory
        modelBuilder.Entity<RuneHistory>(entity =>
        {
            entity.Property(e => e.EffectsAfter)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<JsonElement>(v, jsonOptions).ToString());
        });
    }
}