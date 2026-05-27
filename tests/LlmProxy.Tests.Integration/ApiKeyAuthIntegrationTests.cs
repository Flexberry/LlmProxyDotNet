using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LlmProxy.Core.Entities;
using LlmProxy.Core.Utils;
using LlmProxy.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LlmProxy.Tests.Integration;

public class ApiKeyAuthIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ApiKeyAuthIntegrationTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory(dbFixture);
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ChatCompletion_WithValidApiKey_ReturnsOk()
    {
        // Arrange
        var plaintextKey = KeyHelper.GenerateApiKey("sk");
        var hash = KeyHelper.HashKey(plaintextKey);
        
        await _dbFixture.DbContext!.ApiKeys.AddAsync(new ApiKey
        {
            KeyHash = hash,
            Permissions = "*",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _dbFixture.DbContext.SaveChangesAsync();

        var request = new { model = "openai/gpt-4o", messages = new[] { new { role = "user", content = "test" } } };
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plaintextKey);

        // Act - мокаем внешний вызов через HttpMessageHandler в factory
        // Для интеграционного теста достаточно проверить, что запрос проходит аутентификацию
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert - ожидаем не 401 (аутентификация прошла), дальнейшая логика зависит от мока провайдера
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChatCompletion_WithInvalidApiKey_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "sk_invalid_key");
        var request = new { model = "openai/gpt-4o", messages = new[] { new { role = "user", content = "test" } } };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminKeys_WithMasterKey_ReturnsOk()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-Admin-Key", "sk_master_dev_001");

        // Act
        var response = await _client.GetAsync("/admin/keys");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}