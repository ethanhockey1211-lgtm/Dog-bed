using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages;

public class BathBrushModel : PageModel
{
    private readonly ProductService _productService;

    public BathBrushModel(ProductService productService)
    {
        _productService = productService;
    }

    public Product? Product { get; set; }
    public List<Product> CrossSells { get; set; } = [];

    public void OnGet()
    {
        Product = _productService.GetById(4);
        CrossSells = _productService.GetAll().Where(p => p.Id != 4).ToList();
    }

    public IActionResult OnPostAddToCart(int productId, string size, int quantity)
    {
        var product = _productService.GetById(productId);
        if (product == null) return BadRequest();

        var variant = product.Variants.FirstOrDefault(v => v.Size == size)
                      ?? product.Variants.FirstOrDefault();
        if (variant == null) return BadRequest();

        var item = new CartItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Size = variant.Size,
            Color = variant.Color,
            DimensionsCm = variant.DimensionsCm,
            UnitPrice = variant.Price,
            OriginalUnitPrice = variant.OriginalPrice,
            Quantity = Math.Max(1, quantity),
            Image = product.Images.FirstOrDefault() ?? "/images/placeholder.svg"
        };

        CartService.AddItem(HttpContext.Session, item);
        return new OkResult();
    }
}
