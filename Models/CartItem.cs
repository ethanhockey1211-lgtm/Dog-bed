namespace DogBed.Models;

public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Size { get; set; } = "";
    public string Color { get; set; } = "";
    public string DimensionsCm { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class Cart
{
    public List<CartItem> Items { get; set; } = new();
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public decimal Shipping => 0m;
    public decimal Total => Subtotal + Shipping;
    public int ItemCount => Items.Sum(i => i.Quantity);
}
