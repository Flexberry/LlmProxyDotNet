using LlmProxy.Core.Entities;
using LlmProxy.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LlmProxy.Infrastructure.Logging;

public class LogBatchWriterService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogBatchWriterService> _logger;
    private readonly TimeSpan _flushInterval;
    private readonly int _batchSize;

    // Канал теперь статический или передается через DI, но проще создать его здесь
    private static readonly Channel<RequestLog> SharedChannel = Channel.CreateBounded<RequestLog>(new BoundedChannelOptions(1000)
    {
        SingleWriter = false,
        SingleReader = true,
        FullMode = BoundedChannelFullMode.DropOldest
    });

    public LogBatchWriterService(
        IServiceScopeFactory scopeFactory,
        ILogger<LogBatchWriterService> logger,
        TimeSpan? flushInterval = null,
        int batchSize = 50)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _flushInterval = flushInterval ?? TimeSpan.FromSeconds(10);
        _batchSize = batchSize;
    }

    // Статический метод для получения Writer'а другими сервисами
    public static ChannelWriter<RequestLog> GetWriter() => SharedChannel.Writer;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<RequestLog>(_batchSize);
        var timer = new PeriodicTimer(_flushInterval);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                bool hasItems = false;
                
                // Ждем появления элементов или тика таймера
                while (batch.Count < _batchSize)
                {
                    if (SharedChannel.Reader.TryRead(out var log))
                    {
                        batch.Add(log);
                        hasItems = true;
                    }
                    else
                    {
                        break; 
                    }
                }

                if (hasItems || await timer.WaitForNextTickAsync(stoppingToken))
                {
                    if (batch.Count > 0)
                    {
                        await FlushBatchAsync(batch, stoppingToken);
                        batch.Clear();
                    }
                }
            }

            // Финальная отправка
            if (batch.Count > 0)
            {
                await FlushBatchAsync(batch, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("LogBatchWriterService stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LogBatchWriterService");
        }
    }

    private async Task FlushBatchAsync(List<RequestLog> batch, CancellationToken ct)
    {
        // Создаем Scope для каждого батча, чтобы получить DbContext
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LlmProxyDbContext>();

        try
        {
            await dbContext.RequestLogs.AddRangeAsync(batch, ct);
            await dbContext.SaveChangesAsync(ct);
            _logger.LogDebug("Flushed {Count} log entries to database", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush {Count} log entries", batch.Count);
        }
    }
}