using DogBed.Models;
using DogBed.Services;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout   = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly  = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddSingleton<ProductService>();
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton<OrderQueue>();
builder.Services.AddSingleton<FulfillmentService>();
builder.Services.AddSingleton<StripeCheckoutService>();
builder.Services.AddHostedService<OrderQueueWorker>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();

// Stripe webhook — reads raw body before any middleware can consume it
app.MapPost("/api/stripe-webhook", async (
    HttpRequest req,
    OrderQueue queue,
    OrderStore store,
    IConfiguration config,
    ILogger<Program> logger) =>
{
    var payload = await new StreamReader(req.Body).ReadToEndAsync();
    var sig     = req.Headers["Stripe-Signature"].ToString();
    var secret  = config["Stripe:WebhookSecret"]!;

    Stripe.Event evt;
    try
    {
        evt = EventUtility.ConstructEvent(payload, sig, secret,
            throwOnApiVersionMismatch: false);
    }
    catch (StripeException ex)
    {
        logger.LogWarning("Invalid Stripe signature: {Msg}", ex.Message);
        return Results.BadRequest(new { error = ex.Message });
    }

    if (evt.Type == EventTypes.CheckoutSessionCompleted)
    {
        var session = evt.Data.Object as Session;
        if (session?.PaymentStatus == "paid")
        {
            var order = BuildOrder(session);
            store.Save(order);
            await queue.EnqueueAsync(order);
            logger.LogInformation("New paid order {Id} queued", order.Id);
        }
    }

    return Results.Ok();
}).ExcludeFromDescription();

app.Run();

// Reconstruct a FulfillmentOrder from the Stripe session metadata
static FulfillmentOrder BuildOrder(Session session)
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
            catch { /* skip malformed */ }
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
