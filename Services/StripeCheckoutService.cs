using DogBed.Models;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

namespace DogBed.Services;

public class StripeCheckoutService
{
    public StripeCheckoutService(IConfiguration config)
    {
        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
    }

    public async Task<string> CreateSessionAsync(Cart cart, Order shipping, string baseUrl)
    {
        var lineItems = cart.Items.Select(item => new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = "usd",
                UnitAmountDecimal = item.UnitPrice * 100,
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = $"PET HEAVEN Dog Bed — Size {item.Size}",
                    Description = $"{item.DimensionsCm} · {item.Color}"
                }
            },
            Quantity = item.Quantity
        }).ToList();

        // Pack shipping address + cart into Stripe metadata so webhook can rebuild the order
        var meta = new Dictionary<string, string>
        {
            ["FirstName"]  = shipping.FirstName,
            ["LastName"]   = shipping.LastName,
            ["Email"]      = shipping.Email,
            ["Phone"]      = shipping.Phone,
            ["Address"]    = shipping.Address,
            ["City"]       = shipping.City,
            ["State"]      = shipping.State,
            ["ZipCode"]    = shipping.ZipCode,
            ["Country"]    = shipping.Country,
            ["ItemCount"]  = cart.Items.Count.ToString()
        };

        for (int i = 0; i < cart.Items.Count; i++)
        {
            var it = cart.Items[i];
            // Keep under Stripe's 500-char metadata value limit
            meta[$"Item_{i}"] = JsonSerializer.Serialize(new
            {
                it.ProductId, it.ProductName, it.Size,
                it.Color, it.DimensionsCm, it.UnitPrice, it.Quantity
            });
        }

        // Minnesota sales tax 6.875% — only for MN customers
        var state = shipping.State?.Trim().ToUpper();
        if (state == "MN" || state == "MINNESOTA")
        {
            var taxAmount = Math.Round(cart.Total * 0.06875m, 2);
            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency  = "usd",
                    UnitAmountDecimal = taxAmount * 100,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Minnesota Sales Tax (6.875%)"
                    }
                },
                Quantity = 1
            });
        }

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems          = lineItems,
            Mode               = "payment",
            CustomerEmail      = shipping.Email,
            Metadata           = meta,
            SuccessUrl         = $"{baseUrl}/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl          = $"{baseUrl}/checkout"
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return session.Url!;
    }
}
