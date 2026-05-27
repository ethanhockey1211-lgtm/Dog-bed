using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Stripe.Checkout;

namespace DogBed.Pages;

public class SuccessModel : PageModel
{
    private readonly OrderStore _store;
    private readonly IConfiguration _config;

    public SuccessModel(OrderStore store, IConfiguration config)
    {
        _store  = store;
        _config = config;
    }

    public FulfillmentOrder? Order { get; private set; }
    public string? CustomerEmail { get; private set; }
    public string OrderRef { get; private set; } = "";

    public async Task OnGetAsync(string session_id)
    {
        // Order may already be in the store if the webhook fired first
        Order = _store.GetBySessionId(session_id);

        // Retrieve session from Stripe for display info
        try
        {
            var svc     = new SessionService();
            var session = await svc.GetAsync(session_id);
            CustomerEmail = session.CustomerEmail;
            OrderRef      = session_id[^8..].ToUpper();

            // If the webhook hasn't fired yet, build a lightweight placeholder
            if (Order == null)
            {
                var meta  = session.Metadata;
                OrderRef  = session_id[^8..].ToUpper();
                Order     = new FulfillmentOrder
                {
                    StripeSessionId = session_id,
                    AmountPaid      = (session.AmountTotal ?? 0) / 100m,
                    Address         = meta.GetValueOrDefault("Address") ?? "",
                    City            = meta.GetValueOrDefault("City") ?? "",
                    ZipCode         = meta.GetValueOrDefault("ZipCode") ?? ""
                };
            }
        }
        catch
        {
            OrderRef = session_id.Length >= 8 ? session_id[^8..].ToUpper() : session_id;
        }
    }
}
