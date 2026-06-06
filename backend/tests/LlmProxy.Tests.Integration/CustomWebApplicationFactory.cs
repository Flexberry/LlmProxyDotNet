using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using LlmProxy.Infrastructure.Data;

namespace LlmProxy.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly TestDatabaseFixture? _dbFixture;

    public CustomWebApplicationFactory(TestDatabaseFixture? dbFixture = null)
    {
        _dbFixture = dbFixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        
        // Устанавливаем мастер-ключ и connection string для тестов
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["LITELLM_MASTER_KEY"] = "sk_master_dev_001",
            };
            
            // Если есть dbFixture - используем её connection string
            if (_dbFixture != null && !string.IsNullOrEmpty(_dbFixture.ConnectionString))
            {
                settings["ConnectionStrings:DefaultConnection"] = _dbFixture.ConnectionString;
                settings["RedisConnection"] = _dbFixture.RedisConnection;
            }
            
            config.AddInMemoryCollection(settings);
        });
        
        // Перехватываем сервисы для использования тестовой БД
        builder.ConfigureServices(services =>
        {
            // Убираем существующий DbContext и добавляем тестовый
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LlmProxyDbContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            
            // Если есть dbFixture - используем её строку подключения
            if (_dbFixture != null && !string.IsNullOrEmpty(_dbFixture.ConnectionString))
            {
                services.AddDbContext<LlmProxyDbContext>(options =>
                {
                    options.UseNpgsql(_dbFixture.ConnectionString);
                });
            }
        });
    }
    
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Key", "sk_master_dev_001");
        return client;
    }
}
