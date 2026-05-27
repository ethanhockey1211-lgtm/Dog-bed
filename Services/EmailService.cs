using DogBed.Models;
using System.Text;
using System.Text.Json;

namespace DogBed.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _http;

    public EmailService(IConfiguration config, ILogger<EmailService> logger, IHttpClientFactory httpFactory)
    {
        _config = config;
        _logger = logger;
        _http   = httpFactory.CreateClient("resend");
    }

    public async Task SendOrderNotificationAsync(FulfillmentOrder order)
    {
        var items = string.Join("\n", order.Items.Select(i =>
            $"  • Size {i.Size} ({i.DimensionsCm}) x{i.Quantity} — ${i.LineTotal:F2}"));

        await SendAsync(
            to: _config["Email:NotifyTo"] ?? "",
            subject: $"New Order #{order.StripeSessionId[^8..].ToUpper()} — ${order.AmountPaid:F2}",
            body: $"""
                NEW ORDER — PET HEAVEN
                ══════════════════════════════════
                Order Ref:  #{order.StripeSessionId[^8..].ToUpper()}
                Date:       {order.CreatedAt:MMM d, yyyy h:mm tt} UTC
                Amount:     ${order.AmountPaid:F2}

                CUSTOMER
                Name:   {order.CustomerName}
                Email:  {order.CustomerEmail}
                Phone:  {order.Phone}

                SHIP TO
                {order.Address}
                {order.City}, {order.State} {order.ZipCode}
                {order.Country}

                ITEMS
                {items}
                ══════════════════════════════════
                Go to AliExpress and ship to the address above.
                """);
    }

    public async Task SendBuyerConfirmationAsync(FulfillmentOrder order)
    {
        if (string.IsNullOrEmpty(order.CustomerEmail)) return;

        var firstName = order.CustomerName.Split(' ')[0];
        var items = string.Join("\n", order.Items.Select(i =>
            $"  • Size {i.Size} x{i.Quantity} — ${i.LineTotal:F2}"));

        await SendAsync(
            to: order.CustomerEmail,
            subject: $"Your PET HEAVEN order #{order.StripeSessionId[^8..].ToUpper()} is confirmed!",
            body: $"""
                Hi {firstName},

                Thank you for your order! Your PET HEAVEN dog bed is on its way.

                ORDER SUMMARY
                ══════════════════════════════════
                Order #:  {order.StripeSessionId[^8..].ToUpper()}
                Total:    ${order.AmountPaid:F2}

                {items}

                SHIPPING TO
                {order.Address}
                {order.City}, {order.State} {order.ZipCode}

                ══════════════════════════════════
                Questions? Email us at n10dee23@gmail.com

                — The PET HEAVEN Team
                """);
    }

    private async Task SendAsync(string to, string subject, string body)
    {
        var apiKey  = _config["Email:ResendApiKey"];
        var fromAddr = _config["Email:FromAddress"] ?? "orders@petheavensupply.com";

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(to))
        {
            _logger.LogWarning("Email not configured — skipping (ResendApiKey or NotifyTo missing)");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            from    = $"PET HEAVEN <{fromAddr}>",
            to      = new[] { to },
            subject,
            text    = body
        });

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Headers = { { "Authorization", $"Bearer {apiKey}" } },
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        try
        {
            var resp = await _http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
                _logger.LogInformation("Email sent to {To}", to);
            else
            {
                var err = await resp.Content.ReadAsStringAsync();
                _logger.LogError("Resend API error {Status}: {Body}", resp.StatusCode, err);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }
}
