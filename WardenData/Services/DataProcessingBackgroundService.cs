using Microsoft.Extensions.Caching.Distributed;
using WardenData.Models;

namespace WardenData.Services;

public class DataProcessingBackgroundService : BackgroundService
{
    private readonly IQueueService<QueueItem> _queueService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataProcessingBackgroundService> _logger;

    public DataProcessingBackgroundService(
        IQueueService<QueueItem> queueService,
        IServiceProvider serviceProvider,
        ILogger<DataProcessingBackgroundService> logger)
    {
        _queueService = queueService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data Processing Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queueItem = await _queueService.DequeueAsync(stoppingToken);
                await ProcessQueueItem(queueItem);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue item");
                // Continue processing other items
            }
        }

        _logger.LogInformation("Data Processing Background Service stopped");
    }

    private async Task ProcessQueueItem(QueueItem queueItem)
    {
        using var scope = _serviceProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dataConverter = scope.ServiceProvider.GetRequiredService<IDataConverter>();

        _logger.LogDebug("Processing queue item {Id} of type {Type}", queueItem.Id, queueItem.Type);

        try
        {
            switch (queueItem.Type)
            {
                case "Session":
                    await ProcessSessionData(queueItem.Id, cache, context, dataConverter);
                    break;
                case "Order":
                    await ProcessOrderData(queueItem.Id, cache, context, dataConverter);
                    break;
                case "OrderEffect":
                    await ProcessOrderEffectData(queueItem.Id, cache, context, dataConverter);
                    break;
                case "RuneHistory":
                    await ProcessRuneHistoryData(queueItem.Id, cache, context, dataConverter);
                    break;
                default:
                    _logger.LogWarning("Unknown queue item type: {Type}", queueItem.Type);
                    return;
            }

            // Remove from cache after successful processing
            await cache.RemoveAsync(queueItem.Id);
            _logger.LogDebug("Successfully processed and cleaned up queue item {Id}", queueItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process queue item {Id} of type {Type}", queueItem.Id, queueItem.Type);
            // TODO: Implement retry logic or dead letter queue
            throw;
        }
    }

    private async Task ProcessSessionData(string queueItemId, IDistributedCache cache, AppDbContext context, IDataConverter dataConverter)
    {
        var cachedData = await cache.GetStringAsync(queueItemId);
        if (cachedData == null)
        {
            _logger.LogWarning("No cached data found for session queue item {Id}", queueItemId);
            return;
        }

        await dataConverter.ProcessSessionDataAsync(cachedData, context);
    }

    private async Task ProcessOrderData(string queueItemId, IDistributedCache cache, AppDbContext context, IDataConverter dataConverter)
    {
        var cachedData = await cache.GetStringAsync(queueItemId);
        if (cachedData == null)
        {
            _logger.LogWarning("No cached data found for order queue item {Id}", queueItemId);
            return;
        }

        await dataConverter.ProcessOrderDataAsync(cachedData, context);
    }

    private async Task ProcessOrderEffectData(string queueItemId, IDistributedCache cache, AppDbContext context, IDataConverter dataConverter)
    {
        var cachedData = await cache.GetStringAsync(queueItemId);
        if (cachedData == null)
        {
            _logger.LogWarning("No cached data found for order effect queue item {Id}", queueItemId);
            return;
        }

        await dataConverter.ProcessOrderEffectDataAsync(cachedData, context);
    }

    private async Task ProcessRuneHistoryData(string queueItemId, IDistributedCache cache, AppDbContext context, IDataConverter dataConverter)
    {
        var cachedData = await cache.GetStringAsync(queueItemId);
        if (cachedData == null)
        {
            _logger.LogWarning("No cached data found for rune history queue item {Id}", queueItemId);
            return;
        }

        await dataConverter.ProcessRuneHistoryDataAsync(cachedData, context);
    }
}