using LlmProxy.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LlmProxy.Infrastructure.Data;

public class LlmProxyDbContext : DbContext
{
    public LlmProxyDbContext(DbContextOptions<LlmProxyDbContext> options) : base(options) { }

    // ИСПРАВЛЕНИЕ: Добавлен virtual для поддержки моков в тестах
    public virtual DbSet<ApiKey> ApiKeys { get; set; } = null!;
    public virtual DbSet<RequestLog> RequestLogs { get; set; } = null!;
    public virtual DbSet<Budget> Budgets { get; set; } = null!;
    public virtual DbSet<Team> Teams { get; set; } = null!;
    public virtual DbSet<TeamMember> TeamMembers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Конфигурация ApiKey
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.KeyHash).IsUnique();
            entity.Property(e => e.KeyHash).HasMaxLength(64);
            entity.Property(e => e.Permissions).HasMaxLength(500);
            entity.Property(e => e.RateLimitConfigJson).HasMaxLength(1000);
            
            // Связь с Team
            entity.HasOne(e => e.Team)
                  .WithMany()
                  .HasForeignKey(e => e.TeamId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Конфигурация RequestLog
        modelBuilder.Entity<RequestLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ApiKeyHash);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.ProviderName).HasMaxLength(50);
        });

        // Конфигурация Budget
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.EntityType });
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.EntityType).HasMaxLength(50);
        });

        // Конфигурация Team
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.HasMany(e => e.Members)
                  .WithOne(m => m.Team)
                  .HasForeignKey(m => m.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Конфигурация TeamMember
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.Role).HasMaxLength(20);
        });
    }
}