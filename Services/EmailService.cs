using DogBed.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

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
        var items = string.Join("\n", order.Items.Select(i =>
            $"  • Size {i.Size} ({i.DimensionsCm}) x{i.Quantity} — ${i.LineTotal:F2}"));

        var body = $"""
            NEW ORDER — PET HEAVEN
            ══════════════════════════════════
            Order Ref:  #{order.StripeSessionId[^8..].ToUpper()}
            Date:       {order.CreatedAt:MMM d, yyyy h:mm tt} UTC
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
            Go to AliExpress and ship to the address above.
            """;

        await SendAsync(
            to: _config["Email:NotifyTo"] ?? _config["Email:GmailUser"]!,
            subject: $"🛒 New Order #{order.StripeSessionId[^8..].ToUpper()} — ${order.AmountPaid:F2}",
            body: body);
    }

    public async Task SendBuyerConfirmationAsync(FulfillmentOrder order)
    {
        if (string.IsNullOrEmpty(order.CustomerEmail)) return;

        var items = string.Join("\n", order.Items.Select(i =>
            $"  • Size {i.Size} x{i.Quantity} — ${i.LineTotal:F2}"));

        var body = $"""
            Hi {order.CustomerName.Split(' ')[0]},

            Thank you for your order! 🐾 We've received your payment and are preparing your PET HEAVEN dog bed for shipment.

            ORDER SUMMARY
            ══════════════════════════════════
            Order Ref:  #{order.StripeSessionId[^8..].ToUpper()}
            Amount:     ${order.AmountPaid:F2}

            {items}

            SHIPPING TO
            {order.Address}
            {order.City}, {order.State} {order.ZipCode}

            ══════════════════════════════════
            You'll receive a shipping confirmation once your order is on its way.
            Questions? Reply to this email.

            — The PET HEAVEN Team
            """;

        await SendAsync(
            to: order.CustomerEmail,
            subject: $"Your PET HEAVEN order #{order.StripeSessionId[^8..].ToUpper()} is confirmed!",
            body: body);
    }

    private async Task SendAsync(string to, string subject, string body)
    {
        var gmailUser = _config["Email:GmailUser"];
        var gmailPass = _config["Email:GmailAppPassword"];

        if (string.IsNullOrEmpty(gmailUser) || string.IsNullOrEmpty(gmailPass))
        {
            _logger.LogWarning("Email not configured — skipping notification");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("PET HEAVEN", gmailUser));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(gmailUser, gmailPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Msg}", to, ex.Message);
        }
    }
}
