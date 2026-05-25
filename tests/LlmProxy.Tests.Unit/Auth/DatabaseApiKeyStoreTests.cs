// tests/LlmProxy.Tests.Unit/Auth/DatabaseApiKeyStoreTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LlmProxy.Core.Entities;
using LlmProxy.Core.Utils;
using LlmProxy.Infrastructure.Auth;
using LlmProxy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace LlmProxy.Tests.Unit.Auth;

public class DatabaseApiKeyStoreTests
{
    private readonly Mock<LlmProxyDbContext> _mockDb;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockRedisDb;
    private readonly DatabaseApiKeyStore _store;

    public DatabaseApiKeyStoreTests()
    {
        _mockDb = new Mock<LlmProxyDbContext>(new DbContextOptions<LlmProxyDbContext>());
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockRedisDb = new Mock<IDatabase>();
        
        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockRedisDb.Object);
        
        _store = new DatabaseApiKeyStore(_mockDb.Object, _mockRedis.Object);
    }

    private Mock<DbSet<ApiKey>> CreateMockDbSet(List<ApiKey> data)
    {
        var queryableData = data.AsQueryable();
        var mockSet = new Mock<DbSet<ApiKey>>();
        
        // Настраиваем IAsyncEnumerable для поддержки async foreach
        mockSet.As<IAsyncEnumerable<ApiKey>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<ApiKey>(queryableData.GetEnumerator()));
        
        // Настраиваем IQueryable с нашим кастомным провайдером
        mockSet.As<IQueryable<ApiKey>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<ApiKey>(queryableData.Provider));
        mockSet.As<IQueryable<ApiKey>>().Setup(m => m.Expression).Returns(queryableData.Expression);
        mockSet.As<IQueryable<ApiKey>>().Setup(m => m.ElementType).Returns(queryableData.ElementType);
        mockSet.As<IQueryable<ApiKey>>().Setup(m => m.GetEnumerator()).Returns(queryableData.GetEnumerator());
        
        // Настраиваем FindAsync
        mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object[] ids, CancellationToken ct) => 
                data.FirstOrDefault(k => k.Id.Equals(ids[0]) || k.KeyHash.Equals(ids[0])));

        return mockSet;
    }

    [Fact]
    public async Task GetByKeyHashAsync_ReturnsFromCache_WhenKeyExists()
    {
        var key = new ApiKey { KeyHash = "hash123", IsActive = true, Permissions = "*" };
        var cachedJson = System.Text.Json.JsonSerializer.Serialize(key);
        
        _mockRedisDb.Setup(db => db.StringGetAsync("api_key:hash123", It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(cachedJson));

        var result = await _store.GetByKeyHashAsync("hash123");

        Assert.NotNull(result);
        Assert.Equal("hash123", result!.KeyHash);
    }

    [Fact]
    public async Task GetByKeyHashAsync_FetchesFromDatabase_WhenNotInCache()
    {
        var key = new ApiKey { KeyHash = "hash456", IsActive = true, Permissions = "model1,model2" };
        var mockSet = CreateMockDbSet(new List<ApiKey> { key });
        
        _mockDb.Setup(db => db.ApiKeys).Returns(mockSet.Object);
        _mockRedisDb.Setup(db => db.StringGetAsync("api_key:hash456", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var result = await _store.GetByKeyHashAsync("hash456");

        Assert.NotNull(result);
        Assert.Equal("hash456", result!.KeyHash);
    }

    [Fact]
    public async Task GetByKeyHashAsync_ReturnsNull_ForInactiveKey()
    {
        var key = new ApiKey { KeyHash = "hash789", IsActive = false };
        var mockSet = CreateMockDbSet(new List<ApiKey> { key });
        
        _mockDb.Setup(db => db.ApiKeys).Returns(mockSet.Object);
        _mockRedisDb.Setup(db => db.StringGetAsync("api_key:hash789", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var result = await _store.GetByKeyHashAsync("hash789");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_AddsKeyToDatabase()
    {
        var key = new ApiKey { KeyHash = "newhash", IsActive = true };
        var mockSet = new Mock<DbSet<ApiKey>>();
        
        _mockDb.Setup(db => db.ApiKeys).Returns(mockSet.Object);
        _mockDb.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _store.CreateAsync(key);

        Assert.Equal(key, result);
        mockSet.Verify(m => m.Add(key), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_DeactivatesKeyAndInvalidatesCache()
    {
        var key = new ApiKey { Id = Guid.NewGuid(), KeyHash = "torevoke", IsActive = true };
        var mockSet = CreateMockDbSet(new List<ApiKey> { key });
        
        _mockDb.Setup(db => db.ApiKeys).Returns(mockSet.Object);
        _mockDb.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _store.RevokeAsync(key.Id);

        Assert.True(result);
        Assert.False(key.IsActive);
        _mockRedisDb.Verify(db => db.KeyDeleteAsync("api_key:torevoke", It.IsAny<CommandFlags>()), Times.Once);
    }
}

// --- Реализация IAsyncQueryProvider для EF Core (ИСПРАВЛЕНО: ExecuteAsync возвращает TResult) ---

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
    {
        return new TestAsyncEnumerable<TResult>(expression);
    }

    // ИСПРАВЛЕНИЕ: Возвращаем TResult, как требует ошибка компиляции
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        return _inner.Execute<TResult>(expression);
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        return GetAsyncEnumerator(cancellationToken);
    }
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return new ValueTask();
    }
}