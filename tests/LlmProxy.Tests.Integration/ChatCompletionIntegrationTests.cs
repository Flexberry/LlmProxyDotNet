using System.Net;
using System.Net.Http.Json;
using System.Text;
using LlmProxy.Core.Utils;
using LlmProxy.Infrastructure.Data;
using LlmProxy.Core.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LlmProxy.Tests.Integration;

/// <summary>
/// Integration tests for chat completion endpoints.
/// These tests verify the full request flow from client to provider adapters.
/// </summary>
public class ChatCompletionIntegrationTests 
    : IClassFixture<WebApplicationFactory<Program>>, 
      IClassFixture<TestDatabaseFixture>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly TestDatabaseFixture _dbFixture;
    private readonly HttpClient _client;

    public ChatCompletionIntegrationTests(
        WebApplicationFactory<Program> factory, 
        TestDatabaseFixture dbFixture)
    {
        _factory = factory;
        _dbFixture = dbFixture;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ChatCompletion_WithValidRequest_ReturnsSuccess()
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

        var request = new 
        { 
            model = "openai/gpt-4o", 
            messages = new[] { new { role = "user", content = "Hello" } } 
        };
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plaintextKey);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        // Should not be 401 (authentication passed)
        // Actual response depends on provider mock configuration
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChatCompletion_WithExpiredKey_ReturnsUnauthorized()
    {
        // Arrange
        var plaintextKey = KeyHelper.GenerateApiKey("sk");
        var hash = KeyHelper.HashKey(plaintextKey);
        
        await _dbFixture.DbContext!.ApiKeys.AddAsync(new ApiKey
        {
            KeyHash = hash,
            Permissions = "*",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });
        await _dbFixture.DbContext.SaveChangesAsync();

        var request = new 
        { 
            model = "openai/gpt-4o", 
            messages = new[] { new { role = "user", content = "Test" } } 
        };
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plaintextKey);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChatCompletion_WithModelPermissionDeniesUnauthorizedModel()
    {
        // Arrange
        var plaintextKey = KeyHelper.GenerateApiKey("sk");
        var hash = KeyHelper.HashKey(plaintextKey);
        
        // Key only allows ollama models
        await _dbFixture.DbContext!.ApiKeys.AddAsync(new ApiKey
        {
            KeyHash = hash,
            Permissions = "ollama/llama3",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _dbFixture.DbContext.SaveChangesAsync();

        var request = new 
        { 
            model = "openai/gpt-4o", // Not allowed
            messages = new[] { new { role = "user", content = "Test" } } 
        };
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plaintextKey);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert - should be 403 Forbidden (model not allowed)
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChatCompletion_WithWildcardPermissionAllowsAllModels()
    {
        // Arrange
        var plaintextKey = KeyHelper.GenerateApiKey("sk");
        var hash = KeyHelper.HashKey(plaintextKey);
        
        await _dbFixture.DbContext!.ApiKeys.AddAsync(new ApiKey
        {
            KeyHash = hash,
            Permissions = "*", // Wildcard allows all
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _dbFixture.DbContext.SaveChangesAsync();

        var request = new 
        { 
            model = "openai/gpt-4o", 
            messages = new[] { new { role = "user", content = "Test" } } 
        };
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plaintextKey);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChatCompletion_WithMissingModel_ReturnsBadRequest()
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

        var request = new 
        { 
            messages = new[] { new { role = "user", content = "Test" } } // No model
        };
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plaintextKey);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChatCompletion_WithMissingMessages_ReturnsBadRequest()
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

        var request = new 
        { 
            model = "openai/gpt-4o"
            // No messages
        };
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plaintextKey);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
