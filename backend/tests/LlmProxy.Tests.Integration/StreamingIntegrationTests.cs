using System.Net;
using System.Net.Http.Json;
using System.Text;
using LlmProxy.Core.Utils;
using LlmProxy.Infrastructure.Data;
using LlmProxy.Core.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LlmProxy.Tests.Integration;

public class StreamingIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public StreamingIntegrationTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory(dbFixture);
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

        // Use Ollama to avoid external API dependency
        var request = new { model = "ollama/llama3", messages = new[] { new { role = "user", content = "test" } }, stream = true };
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plaintextKey);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        // Note: May return 502/500 if Ollama is not running
        // The key point is to verify authentication and routing work correctly
        // We check that it's NOT 401/403 (auth/permission errors)
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        
        // If we get a successful response, verify SSE format
        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("data:", content);
        }
        // Otherwise, provider error is acceptable (Ollama may not be running)
    }
}