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

var builder = WebApplication.CreateBuilder(args);

// 1. EF Core & Redis
builder.Services.AddDbContext<LlmProxyDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration["DATABASE_URL"] ?? builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var cfg = ConfigurationOptions.Parse(builder.Configuration["REDIS_CONNECTION"] ?? "localhost:6379");
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
        var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("LlmClient");
        var log = sp.GetRequiredService<ILogger<T>>();
        if (cfg.Providers.TryGetValue(key, out var settings))
            return (T)Activator.CreateInstance(typeof(T), http, settings, log)!;
        throw new InvalidOperationException($"Provider '{key}' not configured");
    });
}
RegisterProvider<OpenAIAdapter>("openai");
RegisterProvider<OllamaAdapter>("ollama");
RegisterProvider<VllmAdapter>("vllm");
RegisterProvider<OpenRouterAdapter>("openrouter");
RegisterProvider<ZAiAdapter>("zai");

// 5. Core Services
builder.Services.AddSingleton<ProviderFactory>();
builder.Services.AddSingleton<ILlmRouter, SimpleRouter>();
builder.Services.AddScoped<IApiKeyStore, DatabaseApiKeyStore>();

// 6. Logging Service
builder.Services.AddSingleton<ILoggingService, DatabaseLoggingService>();
builder.Services.AddHostedService<LogBatchWriterService>();

// 7. MVC & JSON
builder.Services.AddControllers().AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    opt.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// 1. Добавляем политику CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Разрешаем Next.js
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Apply migrations (Dev only, remove in Prod)
using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<LlmProxyDbContext>().Database.Migrate();

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseMiddleware<ApiKeyAuthMiddleware>();
app.UseMiddleware<ModelPermissionMiddleware>();
app.MapControllers();
app.Run();