using Microsoft.EntityFrameworkCore;
using LlmProxy.Core.Entities;

namespace LlmProxy.Infrastructure.Data;
public class LlmProxyDbContext : DbContext
{
    public LlmProxyDbContext(DbContextOptions<LlmProxyDbContext> options) : base(options) { }
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<RequestLog> RequestLogs => Set<RequestLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.KeyHash).IsUnique();
            b.Property(x => x.Permissions).HasColumnType("jsonb");
        });
        modelBuilder.Entity<RequestLog>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.CreatedAt);
            b.HasIndex(x => x.ApiKeyHash);
        });
    }
}