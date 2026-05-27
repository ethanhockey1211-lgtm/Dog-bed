using DogBed.Models;

namespace DogBed.Services;

public class FulfillmentService
{
    private readonly ILogger<FulfillmentService> _logger;

    public FulfillmentService(ILogger<FulfillmentService> logger)
    {
        _logger = logger;
    }

    public Task FulfillAsync(FulfillmentOrder order, CancellationToken ct)
    {
        // Manual fulfillment — visit /orders to see buyer info and place on AliExpress
        order.Status = FulfillmentStatus.Pending;
        _logger.LogInformation("Order {Id} ready for manual fulfillment", order.Id);
        return Task.CompletedTask;
    }
}
