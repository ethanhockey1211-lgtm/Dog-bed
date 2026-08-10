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
    private readonly EmailService _email;
    private readonly ILogger<SuccessModel> _logger;

    public SuccessModel(OrderStore store, OrderQueue queue, EmailService email, ILogger<SuccessModel> logger)
    {
        _store  = store;
        _queue  = queue;
        _email  = email;
        _logger = logger;
    }

    public FulfillmentOrder? Order { get; private set; }
    public string? CustomerEmail { get; private set; }
    public string OrderRef { get; private set; } = "";

    public async Task OnGetAsync(string session_id)
    {
        if (string.IsNullOrEmpty(session_id)) return;

        OrderRef = session_id.Length >= 8 ? session_id[^8..].ToUpper() : session_id;

        // Already saved (by the webhook or an earlier visit) — emails were sent by whoever created it
        Order = _store.GetBySessionId(session_id);
        if (Order != null)
        {
            CustomerEmail = Order.CustomerEmail;
            ClearCartOnce(session_id);
            return;
        }

        // Retrieve session from Stripe
        Stripe.Checkout.Session? session = null;
        try
        {
            var svc = new SessionService();
            session = await svc.GetAsync(session_id);
            CustomerEmail = session.CustomerEmail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Stripe session {Id}", session_id);
            return;
        }

        if (session.PaymentStatus != "paid")
        {
            _logger.LogWarning("Session {Id} payment status: {Status}", session_id, session.PaymentStatus);
            return;
        }

        // Build and persist the order
        var order = BuildOrder(session);
        _store.Save(order);
        await _queue.EnqueueAsync(order);
        Order = order;
        ClearCartOnce(session_id);

        // Send emails — errors are logged inside EmailService but don't break the page
        await _email.SendOrderNotificationAsync(order);
        await _email.SendBuyerConfirmationAsync(order);
    }

    // Clear the cart once per Stripe session so revisiting an old receipt URL
    // can't wipe a cart the customer has built since.
    private void ClearCartOnce(string sessionId)
    {
        var key = "CartCleared_" + sessionId;
        if (HttpContext.Session.GetString(key) == "1") return;
        CartService.ClearCart(HttpContext.Session);
        HttpContext.Session.SetString(key, "1");
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
