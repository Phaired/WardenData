namespace WardenData.Services;

public interface IQueueService<T>
{
    Task EnqueueAsync(T item);
    Task<T> DequeueAsync(CancellationToken cancellationToken);
}