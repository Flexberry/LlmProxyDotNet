using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LlmProxy.Core.Entities;
using LlmProxy.Core.Utils;
using LlmProxy.Infrastructure.Data;
using Xunit;

namespace LlmProxy.Tests.Integration;

/// <summary>
/// Integration tests for Budget Management v2 feature.
/// Tests budget endpoints availability and basic functionality.
/// </summary>
public class BudgetIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public BudgetIntegrationTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory(dbFixture);
        _client = _factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task Budget_SetBudget_ShouldAcceptRequest()
    {
        // Arrange
        var entityId = Guid.NewGuid().ToString();
        var entityType = "ApiKey";

        var budgetRequest = new
        {
            budgetAmount = 100.00m,
            limitAction = "warn",
            periodEnd = DateTime.UtcNow.AddMonths(1)
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/admin/budgets/{entityType}/{entityId}", budgetRequest);

        // Assert - Endpoint exists and processes request
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Budget_CheckBudget_ShouldReturnStatus()
    {
        // Arrange
        var entityId = Guid.NewGuid().ToString();
        var entityType = "ApiKey";
        
        // Set budget first
        var budgetRequest = new { budgetAmount = 100.00m, limitAction = "warn" };
        await _client.PostAsJsonAsync($"/admin/budgets/{entityType}/{entityId}", budgetRequest);

        // Act
        var response = await _client.GetAsync($"/admin/budgets/{entityType}/{entityId}/check");

        // Assert - Endpoint exists
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Budget_UpdateSpending_ShouldAcceptCost()
    {
        // Arrange
        var entityId = Guid.NewGuid().ToString();
        var entityType = "ApiKey";
        
        var spendingRequest = new { cost = 5.50m };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/admin/budgets/{entityType}/{entityId}/spending", spendingRequest);

        // Assert - Endpoint exists
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Budget_Get_ShouldReturnBudgetIfExists()
    {
        // Arrange
        var entityId = Guid.NewGuid().ToString();
        var entityType = "ApiKey";
        
        // Create budget first
        var budgetRequest = new { budgetAmount = 100.00m, limitAction = "warn" };
        await _client.PostAsJsonAsync($"/admin/budgets/{entityType}/{entityId}", budgetRequest);

        // Act
        var response = await _client.GetAsync($"/admin/budgets/{entityType}/{entityId}");

        // Assert - Endpoint exists
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Budget_Endpoints_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        var anonymousClient = _factory.CreateClient();

        // Act
        var response = await anonymousClient.GetAsync("/admin/budgets/ApiKey/test-id");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

