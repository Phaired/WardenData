using System.Threading.Channels;

namespace WardenData.Services;

public class QueueService<T> : IQueueService<T>
{
    private readonly Channel<T> _channel;
    private readonly ChannelWriter<T> _writer;
    private readonly ChannelReader<T> _reader;

    public QueueService()
    {
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<T>(options);
        _writer = _channel.Writer;
        _reader = _channel.Reader;
    }

    public async Task EnqueueAsync(T item)
    {
        await _writer.WriteAsync(item);
    }

    public async Task<T> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _reader.ReadAsync(cancellationToken);
    }
}