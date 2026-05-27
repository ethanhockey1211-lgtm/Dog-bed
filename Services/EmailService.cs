using DogBed.Models;
using System.Net;
using System.Net.Mail;

namespace DogBed.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendOrderNotificationAsync(FulfillmentOrder order)
    {
        var gmailUser = _config["Email:GmailUser"];
        var gmailPass = _config["Email:GmailAppPassword"];
        var notifyTo  = _config["Email:NotifyTo"] ?? gmailUser;

        if (string.IsNullOrEmpty(gmailUser) || string.IsNullOrEmpty(gmailPass))
        {
            _logger.LogWarning("Email not configured — skipping order notification");
            return;
        }

        var items = string.Join("\n", order.Items.Select(i =>
            $"  • Size {i.Size} ({i.DimensionsCm}) x{i.Quantity} — ${i.LineTotal:F2}"));

        var body = $"""
            NEW ORDER — PET HEAVEN
            ══════════════════════════════════
            Order Ref:  {order.StripeSessionId[^8..].ToUpper()}
            Date:       {order.CreatedAt:MMM d, yyyy h:mm tt}
            Amount:     ${order.AmountPaid:F2}

            CUSTOMER
            Name:       {order.CustomerName}
            Email:      {order.CustomerEmail}
            Phone:      {order.Phone}

            SHIP TO
            {order.Address}
            {order.City}, {order.State} {order.ZipCode}
            {order.Country}

            ITEMS
            {items}
            ══════════════════════════════════
            Go to AliExpress and place this order to the address above.
            """;

        try
        {
            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl   = true,
                Credentials = new NetworkCredential(gmailUser, gmailPass)
            };

            var msg = new MailMessage(gmailUser, notifyTo!)
            {
                Subject = $"🛒 New Order #{order.StripeSessionId[^8..].ToUpper()} — ${order.AmountPaid:F2}",
                Body    = body
            };

            await client.SendMailAsync(msg);
            _logger.LogInformation("Order notification sent for {Id}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order notification email");
        }
    }
}
