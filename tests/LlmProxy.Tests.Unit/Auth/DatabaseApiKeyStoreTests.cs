// tests/LlmProxy.Tests.Unit/Auth/DatabaseApiKeyStoreTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

// Кастомный IAsyncQueryProvider, который обрабатывает Where + FirstOrDefaultAsync
internal class AsyncQueryableExecutor<T> : IAsyncQueryProvider where T : class
{
    private readonly IQueryable<T> _data;

    public AsyncQueryableExecutor(IEnumerable<T> data)
    {
        _data = data.AsQueryable();
    }

    public IQueryable CreateQuery(Expression expression) => _data.Provider.CreateQuery(expression);
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => _data.Provider.CreateQuery<TElement>(expression);
    public object Execute(Expression expression) => ExecuteResult(expression);
    public TResult Execute<TResult>(Expression expression) => (TResult)ExecuteResult(expression)!;
    
    private object ExecuteResult(Expression expression)
    {
        if (expression is MethodCallExpression methodCall)
        {
            var argument = methodCall.Arguments[0];
            
            // Обрабатываем Where
            if (methodCall.Method.Name == nameof(Enumerable.Where) && argument is MethodCallExpression whereCall)
            {
                var source = whereCall.Arguments[0];
                var predicate = (LambdaExpression)whereCall.Arguments[1];
                var filtered = ExecuteResult(source) as IEnumerable<T>;
                var compiled = predicate.Compile();
                filtered = filtered?.Where(x => (bool)compiled.DynamicInvoke(x)!);
                
                // Обрабатываем FirstOrDefault
                if (methodCall.Method.Name == nameof(Enumerable.FirstOrDefault))
                {
                    return filtered?.FirstOrDefault();
                }
                
                return filtered ?? Enumerable.Empty<T>();
            }
            
            // Обрабатываем FirstOrDefault без Where
            if (methodCall.Method.Name == nameof(Enumerable.FirstOrDefault))
            {
                var predicateArg = methodCall.Arguments[1];
                // Предикат может быть обернут в UnaryExpression (Expression.Lambda)
                var predicate = predicateArg is UnaryExpression unary 
                    ? (LambdaExpression)unary.Operand 
                    : (LambdaExpression)predicateArg;
                var compiled = predicate.Compile();
                return _data.FirstOrDefault(x => (bool)compiled.DynamicInvoke(x)!);
            }
        }
        
        return _data;
    }

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression) 
    {
        var result = Execute(expression);
        return result is IEnumerable<TResult> enumerable 
            ? new QueryableAsyncEnumerable<TResult>(enumerable) 
            : new QueryableAsyncEnumerable<TResult>(Enumerable.Empty<TResult>());
    }
    
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) 
    {
        var result = Execute(expression);
        // Обертываем результат в Task, так как EF Core ожидает Task<T>
        if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
        {
            var taskMethod = typeof(Task).GetMethod(nameof(Task.FromResult)).MakeGenericMethod(typeof(T));
            return (TResult)(object)taskMethod.Invoke(null, new[] { result })!;
        }
        return (TResult)(object)result;
    }
}

internal class QueryableAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public QueryableAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public QueryableAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
        => GetAsyncEnumerator(cancellationToken);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;
    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
    public ValueTask DisposeAsync() { _inner.Dispose(); return new ValueTask(); }
}

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
        
        var mockSet = new Mock<DbSet<ApiKey>>();
        var data = new List<ApiKey> { key };
        
        mockSet.As<IQueryable<ApiKey>>().Setup(m => m.Provider).Returns(
            new AsyncQueryableExecutor<ApiKey>(data));
        mockSet.As<IQueryable<ApiKey>>().Setup(m => m.Expression).Returns(data.AsQueryable().Expression);
        mockSet.As<IQueryable<ApiKey>>().Setup(m => m.ElementType).Returns(typeof(ApiKey));
        mockSet.As<IQueryable<ApiKey>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        
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
        // Создаем активный и неактивный ключи
        var activeKey = new ApiKey { KeyHash = "active123", IsActive = true };
        var inactiveKey = new ApiKey { KeyHash = "hash789", IsActive = false };
        
        var mockSet = new Mock<DbSet<ApiKey>>();
        var data = new List<ApiKey> { activeKey, inactiveKey };
        
        mockSet.As<IQueryable<ApiKey>>().Setup(m => m.Provider).Returns(
            new AsyncQueryableExecutor<ApiKey>(data));
        mockSet.As<IQueryable<ApiKey>>().Setup(m => m.Expression).Returns(data.AsQueryable().Expression);
        mockSet.As<IQueryable<ApiKey>>().Setup(m => m.ElementType).Returns(typeof(ApiKey));
        mockSet.As<IQueryable<ApiKey>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        
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
        var mockSet = new Mock<DbSet<ApiKey>>();
        
        mockSet.Setup(x => x.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        _mockDb.Setup(db => db.ApiKeys).Returns(mockSet.Object);
        _mockDb.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _store.RevokeAsync(key.Id);

        Assert.True(result);
        Assert.False(key.IsActive);
        _mockRedisDb.Verify(db => db.KeyDeleteAsync("api_key:torevoke", It.IsAny<CommandFlags>()), Times.Once);
    }
}
