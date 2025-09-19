namespace WardenData.Services;

public class QueueItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = null!;
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
}

public class SessionQueueItem : QueueItem
{
    public SessionQueueItem()
    {
        Type = "Session";
    }
}

public class OrderQueueItem : QueueItem
{
    public OrderQueueItem()
    {
        Type = "Order";
    }
}

public class OrderEffectQueueItem : QueueItem
{
    public OrderEffectQueueItem()
    {
        Type = "OrderEffect";
    }
}

public class RuneHistoryQueueItem : QueueItem
{
    public RuneHistoryQueueItem()
    {
        Type = "RuneHistory";
    }
}