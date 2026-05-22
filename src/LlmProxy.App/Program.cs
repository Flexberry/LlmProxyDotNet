using LlmProxy.Core.Config;
using LlmProxy.Core.Router;
using LlmProxy.Core.Providers;
using LlmProxy.Infrastructure.Data;
using LlmProxy.Infrastructure.Providers;
using LlmProxy.Infrastructure.Providers.OpenAI;
using LlmProxy.Infrastructure.Providers.Ollama;
using LlmProxy.Infrastructure.Providers.Vllm;
using LlmProxy.Infrastructure.Providers.OpenRouter;
using LlmProxy.Infrastructure.Providers.ZAi;
using LlmProxy.Infrastructure.Router;
using LlmProxy.Infrastructure.Auth;
using LlmProxy.Core.Auth;
using LlmProxy.App.Middleware;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// 1. EF Core
builder.Services.AddDbContext<LlmProxyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Redis
var redisConnection = builder.Configuration["REDIS_CONNECTION"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
{
    var config = ConfigurationOptions.Parse(redisConnection);
    config.AbortOnConnectFail = false; // Не падать, если Redis недоступен
    return ConnectionMultiplexer.Connect(config);
});

// 1. Регистрируем конфигурацию
builder.Services.Configure<LlmConfig>(builder.Configuration.GetSection("LlmConfig"));

// 2. Регистрируем HttpClient для провайдеров
builder.Services.AddHttpClient("LlmClient");

// 3. Фабричная регистрация адаптеров
// Мы используем IServiceProvider, чтобы получить IConfiguration и создать адаптеры вручную
builder.Services.AddSingleton<ILlmProvider>(sp =>
{
    var config = sp.GetRequiredService<IOptions<LlmConfig>>().Value;
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<OpenAIAdapter>>();
    
    // Получаем настройки для OpenAI
    if (config.Providers.TryGetValue("openai", out var openAiSettings))
    {
        var client = httpClientFactory.CreateClient("LlmClient");
        return new OpenAIAdapter(client, openAiSettings, logger);
    }
    throw new InvalidOperationException("OpenAI provider settings not found in configuration.");
});

builder.Services.AddSingleton<ILlmProvider>(sp =>
{
    var config = sp.GetRequiredService<IOptions<LlmConfig>>().Value;
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<OllamaAdapter>>();
    
    if (config.Providers.TryGetValue("ollama", out var ollamaSettings))
    {
        var client = httpClientFactory.CreateClient("LlmClient");
        return new OllamaAdapter(client, ollamaSettings, logger);
    }
    throw new InvalidOperationException("Ollama provider settings not found in configuration.");
});

builder.Services.AddSingleton<ILlmProvider>(sp =>
{
    var config = sp.GetRequiredService<IOptions<LlmConfig>>().Value;
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<VllmAdapter>>();
    
    if (config.Providers.TryGetValue("vllm", out var vllmSettings))
    {
        var client = httpClientFactory.CreateClient("LlmClient");
        return new VllmAdapter(client, vllmSettings, logger);
    }
    throw new InvalidOperationException("vLLM provider settings not found in configuration.");
});

builder.Services.AddSingleton<ILlmProvider>(sp =>
{
    var config = sp.GetRequiredService<IOptions<LlmConfig>>().Value;
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<OpenRouterAdapter>>();
    
    if (config.Providers.TryGetValue("openrouter", out var openRouterSettings))
    {
        var client = httpClientFactory.CreateClient("LlmClient");
        return new OpenRouterAdapter(client, openRouterSettings, logger);
    }
    throw new InvalidOperationException("OpenRouter provider settings not found in configuration.");
});

builder.Services.AddSingleton<ILlmProvider>(sp =>
{
    var config = sp.GetRequiredService<IOptions<LlmConfig>>().Value;
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ZAiAdapter>>();
    
    if (config.Providers.TryGetValue("zai", out var zaiSettings))
    {
        var client = httpClientFactory.CreateClient("LlmClient");
        return new ZAiAdapter(client, zaiSettings, logger);
    }
    throw new InvalidOperationException("Z.ai provider settings not found in configuration.");
});

// 4. Регистрация Router и Factory
builder.Services.AddSingleton<ProviderFactory>();
builder.Services.AddSingleton<ILlmRouter, SimpleRouter>();

// === Регистрация аутентификации ===
builder.Services.AddScoped<IApiKeyStore, DatabaseApiKeyStore>();

// 5. MVC + OpenAI-совместимая сериализация
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });


builder.Services.AddScoped<IApiKeyStore, DatabaseApiKeyStore>();

builder.Services.AddEndpointsApiExplorer();



var app = builder.Build();

// Автоприменение миграций (для dev, в prod вынести в отдельный шаг)
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<LlmProxyDbContext>();
db.Database.Migrate();


app.UseAuthorization();
app.UseMiddleware<ApiKeyAuthMiddleware>();
app.UseMiddleware<ModelPermissionMiddleware>();
app.MapControllers();
app.Run();