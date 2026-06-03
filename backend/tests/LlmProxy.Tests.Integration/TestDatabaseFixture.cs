// tests/LlmProxy.Tests.Integration/TestDatabaseFixture.cs
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using LlmProxy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LlmProxy.Tests.Integration;

public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly IContainer _postgres;
    private readonly IContainer _redis;
    
    public LlmProxyDbContext? DbContext { get; private set; }
    public string ConnectionString { get; private set; } = string.Empty;
    public string RedisConnection { get; private set; } = string.Empty;

    public TestDatabaseFixture()
    {
        _postgres = new ContainerBuilder()
            .WithImage("postgres:16-alpine")
            .WithEnvironment("POSTGRES_USER", "test")
            .WithEnvironment("POSTGRES_PASSWORD", "test")
            .WithEnvironment("POSTGRES_DB", "litellm_test")
            .WithPortBinding(5432, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("database system is ready to accept connections"))
            .Build();

        _redis = new ContainerBuilder()
            .WithImage("redis:7-alpine")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("Ready to accept connections"))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();
        
        ConnectionString = $"Host={_postgres.Hostname};Port={_postgres.GetMappedPublicPort(5432)};Database=litellm_test;Username=test;Password=test;SslMode=Disable";
        RedisConnection = $"{_redis.Hostname}:{_redis.GetMappedPublicPort(6379)}";
        
        var options = new DbContextOptionsBuilder<LlmProxyDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        
        DbContext = new LlmProxyDbContext(options);
        
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null) await DbContext.DisposeAsync();
        await _postgres.StopAsync();
        await _redis.StopAsync();
    }
}