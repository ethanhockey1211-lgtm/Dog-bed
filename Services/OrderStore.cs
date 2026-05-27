using System.Collections.Concurrent;
using DogBed.Models;

namespace DogBed.Services;

public class OrderStore
{
    private readonly ConcurrentDictionary<string, FulfillmentOrder> _orders = new();

    public void Save(FulfillmentOrder order) => _orders[order.Id] = order;

    public FulfillmentOrder? GetBySessionId(string sessionId) =>
        _orders.Values.FirstOrDefault(o => o.StripeSessionId == sessionId);

    public IReadOnlyList<FulfillmentOrder> GetAll() =>
        _orders.Values.OrderByDescending(o => o.CreatedAt).ToList();
}
