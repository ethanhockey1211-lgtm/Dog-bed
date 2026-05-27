using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages;

public class CheckoutModel : PageModel
{
    private readonly StripeCheckoutService _stripe;

    public CheckoutModel(StripeCheckoutService stripe)
    {
        _stripe = stripe;
    }

    [BindProperty]
    public Order Order { get; set; } = new();

    public Cart Cart { get; set; } = new();

    public void OnGet()
    {
        Cart = CartService.GetCart(HttpContext.Session);
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
            string.IsNullOrWhiteSpace(Order.Address)   ||
            string.IsNullOrWhiteSpace(Order.City)      ||
            string.IsNullOrWhiteSpace(Order.ZipCode))
        {
            ModelState.AddModelError("", "Please fill in all required fields.");
            return Page();
        }

        var baseUrl    = $"{Request.Scheme}://{Request.Host}";
        var stripeUrl  = await _stripe.CreateSessionAsync(Cart, Order, baseUrl);

        // Clear session cart — Stripe webhook confirms payment before fulfillment
        CartService.ClearCart(HttpContext.Session);

        return Redirect(stripeUrl);
    }
}
