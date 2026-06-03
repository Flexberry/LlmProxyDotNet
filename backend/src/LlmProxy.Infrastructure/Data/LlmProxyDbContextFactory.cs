using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace LlmProxy.Infrastructure.Data;

public class LlmProxyDbContextFactory : IDesignTimeDbContextFactory<LlmProxyDbContext>
{
    public LlmProxyDbContext CreateDbContext(string[] args)
    {
        // Приоритет 1: DATABASE_URL (как в docker-compose)
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        
        // Приоритет 2: ConnectionStrings__DefaultConnection (локальная разработка)
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        }
        
        // Fallback для локальной разработки
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=litellm;Username=user;Password=password";
        }

        var optionsBuilder = new DbContextOptionsBuilder<LlmProxyDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new LlmProxyDbContext(optionsBuilder.Options);
    }
}