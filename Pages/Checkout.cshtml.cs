using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace DogBed.Pages;

public class CheckoutModel : PageModel
{
    private const string ShippingKey = "CheckoutShipping";

    private readonly StripeCheckoutService _stripe;
    private readonly IConfiguration _config;

    public CheckoutModel(StripeCheckoutService stripe, IConfiguration config)
    {
        _stripe = stripe;
        _config = config;
    }

    [BindProperty]
    public Order Order { get; set; } = new();

    [BindProperty]
    public string? PromoCode { get; set; }

    public Cart Cart { get; set; } = new();

    public void OnGet()
    {
        Cart = CartService.GetCart(HttpContext.Session);

        // Prefill shipping info if the buyer came back from Stripe without paying
        var saved = HttpContext.Session.GetString(ShippingKey);
        if (saved != null)
        {
            try { Order = JsonConvert.DeserializeObject<Order>(saved) ?? new Order(); }
            catch { }
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Cart = CartService.GetCart(HttpContext.Session);

        if (!Cart.Items.Any())
            return RedirectToPage("/Cart");

        // Basic server-side validation
        if (string.IsNullOrWhiteSpace(Order.FirstName) ||
            string.IsNullOrWhiteSpace(Order.LastName)  ||
            string.IsNullOrWhiteSpace(Order.Email)     ||
            string.IsNullOrWhiteSpace(Order.Phone)     ||
            string.IsNullOrWhiteSpace(Order.Address)   ||
            string.IsNullOrWhiteSpace(Order.City)      ||
            string.IsNullOrWhiteSpace(Order.State)     ||
            string.IsNullOrWhiteSpace(Order.ZipCode))
        {
            ModelState.AddModelError("", "Please fill in all required fields.");
            return Page();
        }

        // Validate optional promo code
        var discountPct = 0m;
        string? appliedCode = null;
        if (!string.IsNullOrWhiteSpace(PromoCode))
        {
            var validCode = _config["Promo:Code"] ?? "WELCOME10";
            if (string.Equals(PromoCode.Trim(), validCode, StringComparison.OrdinalIgnoreCase))
            {
                var pct = _config.GetValue<decimal?>("Promo:Percent") ?? 10m;
                discountPct = pct / 100m;
                appliedCode = validCode;
            }
            else
            {
                ModelState.AddModelError(nameof(PromoCode), "That discount code isn't valid.");
                return Page();
            }
        }

        // Keep shipping info + cart so nothing is lost if the buyer backs out on Stripe;
        // the cart is cleared on the success page once payment is confirmed.
        HttpContext.Session.SetString(ShippingKey, JsonConvert.SerializeObject(Order));

        var baseUrl   = $"{Request.Scheme}://{Request.Host}";
        var stripeUrl = await _stripe.CreateSessionAsync(Cart, Order, baseUrl, discountPct, appliedCode);

        return Redirect(stripeUrl);
    }
}
