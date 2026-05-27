using LlmProxy.Core.Auth;
using LlmProxy.Core.Config;
using LlmProxy.Core.Logging;
using LlmProxy.Core.Providers;
using LlmProxy.Core.Router;
using LlmProxy.Infrastructure.Auth;
using LlmProxy.Infrastructure.Data;
using LlmProxy.Infrastructure.Logging;
using LlmProxy.Infrastructure.Providers;
using LlmProxy.Infrastructure.Providers.OpenAI;
using LlmProxy.Infrastructure.Providers.Ollama;
using LlmProxy.Infrastructure.Providers.OpenRouter;
using LlmProxy.Infrastructure.Providers.Vllm;
using LlmProxy.Infrastructure.Providers.ZAi;
using LlmProxy.Infrastructure.Router;
using LlmProxy.App.Middleware;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

var builder = WebApplication.CreateBuilder(args);

var logger = NullLogger<Program>.Instance;

// 1. EF Core & Redis
var connectionString = builder.Configuration["DATABASE_URL"] ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LlmProxyDbContext>(opt =>
    opt.UseNpgsql(connectionString));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConn = builder.Configuration["REDIS_CONNECTION"] ?? "localhost:6379";
    var cfg = ConfigurationOptions.Parse(redisConn);
    cfg.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(cfg);
});

// 2. Config
builder.Services.Configure<LlmConfig>(builder.Configuration.GetSection("LlmConfig"));

// 3. HTTP Client
builder.Services.AddHttpClient("LlmClient");

// 4. Providers Registration
void RegisterProvider<T>(string key) where T : class, ILlmProvider
{
    builder.Services.AddSingleton<ILlmProvider>(sp =>
    {
        var cfg = sp.GetRequiredService<IOptions<LlmConfig>>().Value;
        
        // Проверка наличия секции
        if (!cfg.Providers.TryGetValue(key, out var settings))
        {
            // Если провайдера нет в конфиге, логируем и возвращаем null или кидаем исключение
            // Для MVP лучше кидать исключение, чтобы сразу видеть проблему
            throw new InvalidOperationException($"Provider '{key}' is missing in LlmConfig section.");
        }

        // ИСПРАВЛЕНИЕ: Подстановка API ключей из переменных окружения
        if (key == "openai" && string.IsNullOrEmpty(settings.ApiKey))
        {
            settings.ApiKey = builder.Configuration["OPENAI_API_KEY"];
        }
        else if (key == "openrouter" && string.IsNullOrEmpty(settings.ApiKey))
        {
            settings.ApiKey = builder.Configuration["OPENROUTER_API_KEY"];
        }
        else if (key == "zai" && string.IsNullOrEmpty(settings.ApiKey))
        {
            settings.ApiKey = builder.Configuration["ZAI_API_KEY"];
        }

        var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("LlmClient");
        var log = sp.GetRequiredService<ILogger<T>>();
        
        try 
        {
            return (T)Activator.CreateInstance(typeof(T), http, settings, log)!;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create provider '{key}': {ex.Message}", ex);
        }
    });
}

// Регистрируем только те провайдеры, которые есть в конфиге
var llmConfig = builder.Configuration.GetSection("LlmConfig").Get<LlmConfig>() ?? new LlmConfig();
logger.LogInformation($"Loaded providers from config: {string.Join(", ", llmConfig.Providers.Keys)}");

foreach (var providerKey in llmConfig.Providers.Keys)
{
    if (providerKey == "openai")
        RegisterProvider<OpenAIAdapter>("openai");
    else if (providerKey == "ollama")
        RegisterProvider<OllamaAdapter>("ollama");
    else if (providerKey == "vllm")
        RegisterProvider<VllmAdapter>("vllm");
    else if (providerKey == "openrouter")
        RegisterProvider<OpenRouterAdapter>("openrouter");
    else if (providerKey == "zai")
        RegisterProvider<ZAiAdapter>("zai");
    else
        logger.LogWarning($"Unknown provider: {providerKey}");
}

// 5. Core Services
builder.Services.AddSingleton<ProviderFactory>();

// Register OllamaModelService — требуется для ModelsController
builder.Services.AddSingleton<LlmProxy.Infrastructure.Providers.Ollama.OllamaModelService>();

// Register both routers - SimpleRouter is default, LeastBusyRouter available for injection
builder.Services.AddSingleton<ILlmRouter, SimpleRouter>();
builder.Services.AddSingleton<LeastBusyRouter>();

builder.Services.AddScoped<IApiKeyStore, DatabaseApiKeyStore>();

// 6. Logging Service
builder.Services.AddSingleton<ILoggingService, DatabaseLoggingService>();
// ИСПРАВЛЕНИЕ ДАШБОРДА: Уменьшаем интервал до 2 секунд для быстрой отдачи статистики
builder.Services.AddHostedService(sp => 
    new LogBatchWriterService(
        sp.GetRequiredService<IServiceScopeFactory>(),
        sp.GetRequiredService<ILogger<LogBatchWriterService>>(),
        flushInterval: TimeSpan.FromSeconds(2),
        batchSize: 1
    )
);

// 7. MVC & JSON
builder.Services.AddControllers().AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    opt.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// 8. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Разрешаем frontend с любого источника при запуске в Docker
        var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? "http://localhost:3000";
        policy.WithOrigins(frontendUrl, "http://localhost:3000", "http://frontend:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LlmProxyDbContext>();
    db.Database.Migrate();
}

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseMiddleware<ApiKeyAuthMiddleware>();
app.UseMiddleware<ModelPermissionMiddleware>();
app.MapControllers();
app.Run();