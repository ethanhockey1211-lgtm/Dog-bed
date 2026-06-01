using DogBed.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages;

public class ShopModel : PageModel
{
    private readonly ProductService _products;
    public ShopModel(ProductService products) => _products = products;

    public List<ShopItem> Products { get; private set; } = [];

    public void OnGet()
    {
        Products = _products.GetAll().Select(p => new ShopItem
        {
            Name         = p.Name,
            ShortDescription = p.ShortDescription,
            Image        = p.Images.FirstOrDefault() ?? "/images/placeholder.svg",
            PriceLabel   = p.Variants.Count == 1
                ? $"${p.Variants[0].Price:F2}"
                : $"From ${p.Variants.Min(v => v.Price):F2}",
            ReviewCount  = p.ReviewCount,
            Url          = $"/{GetSlug(p.Name)}"
        }).ToList();
    }

    private static string GetSlug(string name) =>
        name.ToLower().Contains("brush") ? "Brush" :
        name.ToLower().Contains("paw") ? "PawButter" : "DogBed";
}

public class ShopItem
{
    public string Name { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string Image { get; set; } = "";
    public string PriceLabel { get; set; } = "";
    public int ReviewCount { get; set; }
    public string Url { get; set; } = "";
}
