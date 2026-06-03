using System.Net;
using System.Net.Http.Json;
using LlmProxy.Core.Entities;
using LlmProxy.Core.Utils;
using LlmProxy.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LlmProxy.Tests.Integration;

public class AdminEndpointsIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AdminEndpointsIntegrationTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory(dbFixture);
        _client = _factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task AdminKeys_WithMasterKey_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/admin/keys");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminKeys_CreateWithMasterKey_Succeeds()
    {
        // Arrange
        var newKeyRequest = new 
        {
            name = "Test Admin Key",
            permissions = new[] { "*" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/admin/keys", newKeyRequest);

        // Assert - принимаем OK или Created (зависит от реализации контроллера)
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK || 
                   response.StatusCode == System.Net.HttpStatusCode.Created);
    }
}
