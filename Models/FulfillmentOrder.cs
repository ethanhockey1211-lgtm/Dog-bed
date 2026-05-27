namespace DogBed.Models;

public class FulfillmentOrder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
    public string StripeSessionId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Country { get; set; } = "United States";
    public List<CartItem> Items { get; set; } = new();
    public decimal AmountPaid { get; set; }
    public FulfillmentStatus Status { get; set; } = FulfillmentStatus.Pending;
    public string? AliExpressOrderId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum FulfillmentStatus
{
    Pending,
    Processing,
    Fulfilled,
    Failed
}
