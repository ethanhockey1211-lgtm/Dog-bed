namespace DogBed.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public List<ProductVariant> Variants { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public List<string> Images { get; set; } = new();
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public List<Review> Reviews { get; set; } = new();
}

public class ProductVariant
{
    public string Size { get; set; } = "";
    public string DimensionsCm { get; set; } = "";
    public string DimensionsInch { get; set; } = "";
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }
    public string Color { get; set; } = "";
    public int Stock { get; set; }
}

public class Review
{
    public string Author { get; set; } = "";
    public int Stars { get; set; }
    public string Comment { get; set; } = "";
    public string Date { get; set; } = "";
    public string? VerifiedPurchase { get; set; }
}
