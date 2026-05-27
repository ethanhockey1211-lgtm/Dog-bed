using System.Threading.Channels;
using DogBed.Models;

namespace DogBed.Services;

public class OrderQueue
{
    private readonly Channel<FulfillmentOrder> _channel =
        Channel.CreateBounded<FulfillmentOrder>(
            new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait });

    public async ValueTask EnqueueAsync(FulfillmentOrder order) =>
        await _channel.Writer.WriteAsync(order);

    public IAsyncEnumerable<FulfillmentOrder> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}

// Background service that drains the queue and calls FulfillmentService
public class OrderQueueWorker : BackgroundService
{
    private readonly OrderQueue _queue;
    private readonly FulfillmentService _fulfillment;
    private readonly OrderStore _store;
    private readonly ILogger<OrderQueueWorker> _logger;

    public OrderQueueWorker(OrderQueue queue, FulfillmentService fulfillment,
        OrderStore store, ILogger<OrderQueueWorker> logger)
    {
        _queue       = queue;
        _fulfillment = fulfillment;
        _store       = store;
        _logger      = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Order fulfillment worker started");

        await foreach (var order in _queue.ReadAllAsync(ct))
        {
            _store.Save(order);
            try
            {
                await _fulfillment.FulfillAsync(order, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fulfillment failed for order {Id}", order.Id);
            }
            finally
            {
                _store.Save(order); // persist updated status
            }
        }
    }
}
