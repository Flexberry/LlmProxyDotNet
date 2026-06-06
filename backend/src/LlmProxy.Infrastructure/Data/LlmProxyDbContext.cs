using LlmProxy.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LlmProxy.Infrastructure.Data;

public class LlmProxyDbContext : DbContext
{
    public LlmProxyDbContext(DbContextOptions<LlmProxyDbContext> options) : base(options) { }

    // ИСПРАВЛЕНИЕ: Добавлен virtual для поддержки моков в тестах
    public virtual DbSet<ApiKey> ApiKeys { get; set; } = null!;
    public virtual DbSet<RequestLog> RequestLogs { get; set; } = null!;

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
    }
}