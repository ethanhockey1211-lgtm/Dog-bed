using DogBed.Models;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

namespace DogBed.Services;

public class StripeCheckoutService
{
    // State-level sales tax rates by abbreviation (0 = no state sales tax)
    private static readonly Dictionary<string, decimal> StateTaxRates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AL"] = 0.04m,
        ["AK"] = 0m,
        ["AZ"] = 0.056m,
        ["AR"] = 0.065m,
        ["CA"] = 0.0725m,
        ["CO"] = 0.029m,
        ["CT"] = 0.0635m,
        ["DE"] = 0m,
        ["FL"] = 0.06m,
        ["GA"] = 0.04m,
        ["HI"] = 0.04m,
        ["ID"] = 0.06m,
        ["IL"] = 0.0625m,
        ["IN"] = 0.07m,
        ["IA"] = 0.06m,
        ["KS"] = 0.065m,
        ["KY"] = 0.06m,
        ["LA"] = 0.0445m,
        ["ME"] = 0.055m,
        ["MD"] = 0.06m,
        ["MA"] = 0.0625m,
        ["MI"] = 0.06m,
        ["MN"] = 0.06875m,
        ["MS"] = 0.07m,
        ["MO"] = 0.04225m,
        ["MT"] = 0m,
        ["NE"] = 0.055m,
        ["NV"] = 0.0685m,
        ["NH"] = 0m,
        ["NJ"] = 0.06625m,
        ["NM"] = 0.05m,
        ["NY"] = 0.04m,
        ["NC"] = 0.0475m,
        ["ND"] = 0.05m,
        ["OH"] = 0.0575m,
        ["OK"] = 0.045m,
        ["OR"] = 0m,
        ["PA"] = 0.06m,
        ["RI"] = 0.07m,
        ["SC"] = 0.06m,
        ["SD"] = 0.045m,
        ["TN"] = 0.07m,
        ["TX"] = 0.0625m,
        ["UT"] = 0.061m,
        ["VT"] = 0.06m,
        ["VA"] = 0.053m,
        ["WA"] = 0.065m,
        ["WV"] = 0.06m,
        ["WI"] = 0.05m,
        ["WY"] = 0.04m,
        ["DC"] = 0.06m,
    };

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
                    Name = item.Size == "One Size"
                        ? item.ProductName
                        : $"{item.ProductName} — Size {item.Size}",
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
            meta[$"Item_{i}"] = JsonSerializer.Serialize(new
            {
                it.ProductId, it.ProductName, it.Size,
                it.Color, it.DimensionsCm, it.UnitPrice, it.Quantity
            });
        }

        // Apply state sales tax based on shipping address
        var stateKey = shipping.State?.Trim().ToUpper() ?? "";
        if (StateTaxRates.TryGetValue(stateKey, out var taxRate) && taxRate > 0)
        {
            var taxAmount = Math.Round(cart.Total * taxRate, 2);
            var taxPct    = (taxRate * 100).ToString("G29");
            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency          = "usd",
                    UnitAmountDecimal = taxAmount * 100,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"{stateKey} Sales Tax ({taxPct}%)"
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
