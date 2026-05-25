using System.Net;
using System.Net.Http.Json;
using System.Text;
using LlmProxy.Core.Utils;
using LlmProxy.Infrastructure.Data;
using LlmProxy.Core.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LlmProxy.Tests.Integration;

public class StreamingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IClassFixture<TestDatabaseFixture>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly TestDatabaseFixture _dbFixture;
    private readonly HttpClient _client;

    public StreamingIntegrationTests(WebApplicationFactory<Program> factory, TestDatabaseFixture dbFixture)
    {
        _factory = factory;
        _dbFixture = dbFixture;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ChatCompletion_Stream_ReturnsSseFormat()
    {
        // Arrange
        var plaintextKey = KeyHelper.GenerateApiKey("sk");
        var hash = KeyHelper.HashKey(plaintextKey);
        
        await _dbFixture.DbContext!.ApiKeys.AddAsync(new ApiKey { KeyHash = hash, Permissions = "*", IsActive = true });
        await _dbFixture.DbContext.SaveChangesAsync();

        var request = new { model = "openai/gpt-4o", messages = new[] { new { role = "user", content = "test" } }, stream = true };
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plaintextKey);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("data:", content); // SSE формат
        Assert.Contains("[DONE]", content); // Финальный маркер
    }
}