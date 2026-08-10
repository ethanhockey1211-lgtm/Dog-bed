namespace DogBed.Models;

public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Size { get; set; } = "";
    public string Color { get; set; } = "";
    public string DimensionsCm { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public int Quantity { get; set; }
    public string Image { get; set; } = "/images/placeholder.svg";
    public decimal LineTotal => UnitPrice * Quantity;
    public decimal LineSavings => OriginalUnitPrice > UnitPrice ? (OriginalUnitPrice - UnitPrice) * Quantity : 0;
}

public class Cart
{
    public List<CartItem> Items { get; set; } = new();
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public decimal Shipping => 0m;
    public decimal Total => Subtotal + Shipping;
    public decimal TotalSavings => Items.Sum(i => i.LineSavings);
    public int ItemCount => Items.Sum(i => i.Quantity);
}
