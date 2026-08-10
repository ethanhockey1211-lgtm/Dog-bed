using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages;

public class OrdersModel : PageModel
{
    private readonly OrderStore _store;
    private readonly ProductService _products;
    private readonly EmailService _email;
    private readonly IConfiguration _config;
    private const string AuthKey = "admin_authed";

    public OrdersModel(OrderStore store, ProductService products, EmailService email, IConfiguration config)
    {
        _store    = store;
        _products = products;
        _email    = email;
        _config   = config;
    }

    public IReadOnlyList<FulfillmentOrder> Orders { get; private set; } = [];
    public bool Authed { get; private set; }
    public string Filter { get; private set; } = "todo";
    public int CountAll { get; private set; }
    public int CountTodo { get; private set; }
    public int CountPlaced { get; private set; }
    public int CountShipped { get; private set; }
    public decimal Revenue { get; private set; }

    public IActionResult OnGet(string? f)
    {
        if (HttpContext.Session.GetString(AuthKey) != "yes")
            return Page(); // show login form
        Authed = true;
        LoadOrders(f);
        return Page();
    }

    public IActionResult OnPost(string password)
    {
        var correct = _config["Admin:Password"];
        if (!string.IsNullOrEmpty(correct) && password == correct)
        {
            HttpContext.Session.SetString(AuthKey, "yes");
            return RedirectToPage();
        }
        ModelState.AddModelError("", "Incorrect password.");
        return Page();
    }

    public IActionResult OnPostMarkPlaced(string sessionId, string? aliOrderId, string? f)
    {
        if (HttpContext.Session.GetString(AuthKey) != "yes") return RedirectToPage();

        var order = _store.GetBySessionId(sessionId);
        if (order != null)
        {
            order.Status = FulfillmentStatus.Processing;
            if (!string.IsNullOrWhiteSpace(aliOrderId))
                order.AliExpressOrderId = aliOrderId.Trim();
            _store.Save(order);
        }
        return RedirectToPage(new { f });
    }

    public async Task<IActionResult> OnPostMarkShippedAsync(string sessionId, string? tracking, string? f)
    {
        if (HttpContext.Session.GetString(AuthKey) != "yes") return RedirectToPage();

        var order = _store.GetBySessionId(sessionId);
        if (order != null)
        {
            order.Status = FulfillmentStatus.Fulfilled;
            order.FulfilledAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(tracking))
                order.TrackingNumber = tracking.Trim();
            _store.Save(order);

            // Let the buyer know their order is on the way (logged, never throws)
            await _email.SendShippingConfirmationAsync(order);
        }
        return RedirectToPage(new { f });
    }

    public string SupplierLink(CartItem item)
    {
        var product = _products.GetById(item.ProductId);
        if (!string.IsNullOrWhiteSpace(product?.SupplierUrl))
            return product.SupplierUrl;

        var name = (product?.Name ?? item.ProductName).Replace("PET HEAVEN", "", StringComparison.OrdinalIgnoreCase);
        var slug = string.Join("-",
            name.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(w => new string(w.Where(char.IsLetterOrDigit).ToArray()))
                .Where(w => w.Length > 0));
        return $"https://www.aliexpress.com/w/wholesale-{slug}.html";
    }

    private void LoadOrders(string? f)
    {
        var all = _store.GetAll();
        CountAll     = all.Count;
        CountTodo    = all.Count(o => o.Status is FulfillmentStatus.Pending or FulfillmentStatus.Failed);
        CountPlaced  = all.Count(o => o.Status == FulfillmentStatus.Processing);
        CountShipped = all.Count(o => o.Status == FulfillmentStatus.Fulfilled);
        Revenue      = all.Sum(o => o.AmountPaid);

        Filter = (f ?? "todo").ToLowerInvariant();
        if (Filter == "todo" && CountTodo == 0) Filter = "all";

        Orders = Filter switch
        {
            "todo"    => all.Where(o => o.Status is FulfillmentStatus.Pending or FulfillmentStatus.Failed).ToList(),
            "placed"  => all.Where(o => o.Status == FulfillmentStatus.Processing).ToList(),
            "shipped" => all.Where(o => o.Status == FulfillmentStatus.Fulfilled).ToList(),
            _         => all
        };
    }
}
