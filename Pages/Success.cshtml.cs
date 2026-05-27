using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Stripe.Checkout;
using System.Text.Json;

namespace DogBed.Pages;

public class SuccessModel : PageModel
{
    private readonly OrderStore _store;
    private readonly OrderQueue _queue;

    public SuccessModel(OrderStore store, OrderQueue queue)
    {
        _store = store;
        _queue = queue;
    }

    public FulfillmentOrder? Order { get; private set; }
    public string? CustomerEmail { get; private set; }
    public string OrderRef { get; private set; } = "";

    public async Task OnGetAsync(string session_id)
    {
        if (string.IsNullOrEmpty(session_id)) return;

        OrderRef = session_id.Length >= 8 ? session_id[^8..].ToUpper() : session_id;

        // Already saved (e.g. from a previous page load)
        Order = _store.GetBySessionId(session_id);
        if (Order != null)
        {
            CustomerEmail = Order.CustomerEmail;
            return;
        }

        try
        {
            var svc     = new SessionService();
            var session = await svc.GetAsync(session_id);
            CustomerEmail = session.CustomerEmail;

            if (session.PaymentStatus != "paid") return;

            // Build and save the order directly — no webhook needed
            var order = BuildOrder(session);
            _store.Save(order);
            await _queue.EnqueueAsync(order);
            Order = order;
        }
        catch { /* Stripe key not set or invalid session — show generic success */ }
    }

    private static FulfillmentOrder BuildOrder(Stripe.Checkout.Session session)
    {
        var m     = session.Metadata;
        var items = new List<CartItem>();

        if (m.TryGetValue("ItemCount", out var cs) && int.TryParse(cs, out var count))
        {
            for (var i = 0; i < count; i++)
            {
                if (!m.TryGetValue($"Item_{i}", out var json)) continue;
                try
                {
                    var d = JsonDocument.Parse(json).RootElement;
                    items.Add(new CartItem
                    {
                        ProductId    = d.GetProperty("productId").GetInt32(),
                        ProductName  = d.GetProperty("productName").GetString() ?? "",
                        Size         = d.GetProperty("size").GetString() ?? "",
                        Color        = d.GetProperty("color").GetString() ?? "",
                        DimensionsCm = d.GetProperty("dimensionsCm").GetString() ?? "",
                        UnitPrice    = d.GetProperty("unitPrice").GetDecimal(),
                        Quantity     = d.GetProperty("quantity").GetInt32()
                    });
                }
                catch { }
            }
        }

        return new FulfillmentOrder
        {
            StripeSessionId = session.Id,
            CustomerName    = $"{m.GetValueOrDefault("FirstName")} {m.GetValueOrDefault("LastName")}".Trim(),
            CustomerEmail   = session.CustomerEmail ?? m.GetValueOrDefault("Email") ?? "",
            Phone           = m.GetValueOrDefault("Phone") ?? "",
            Address         = m.GetValueOrDefault("Address") ?? "",
            City            = m.GetValueOrDefault("City") ?? "",
            State           = m.GetValueOrDefault("State") ?? "",
            ZipCode         = m.GetValueOrDefault("ZipCode") ?? "",
            Country         = m.GetValueOrDefault("Country") ?? "United States",
            Items           = items,
            AmountPaid      = (session.AmountTotal ?? 0) / 100m,
            Status          = FulfillmentStatus.Pending
        };
    }
}
