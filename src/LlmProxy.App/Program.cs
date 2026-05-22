using LlmProxy.Core.Config;
using LlmProxy.Core.Auth;
using LlmProxy.Infrastructure.Data;
using LlmProxy.Infrastructure.Auth;
using LlmProxy.App.Middleware;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using StackExchange.Redis;


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

// 3. Конфигурация
builder.Services.Configure<LlmConfig>(builder.Configuration.GetSection("LlmConfig"));

// 4. HTTP + Resilience
builder.Services.AddHttpClient("LlmClient").AddStandardResilienceHandler();

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