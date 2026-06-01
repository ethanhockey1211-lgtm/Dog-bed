using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages;

public class PawButterModel : PageModel
{
    private readonly ProductService _productService;

    public PawButterModel(ProductService productService)
    {
        _productService = productService;
    }

    public Product? Product { get; set; }

    public void OnGet()
    {
        Product = _productService.GetById(3);
    }

    public IActionResult OnPostAddToCart(int productId, string size, int quantity)
    {
        var product = _productService.GetById(productId);
        if (product == null) return BadRequest();

        var variant = product.Variants.FirstOrDefault(v => v.Size == size);
        if (variant == null) return BadRequest();

        var item = new CartItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Size = size,
            Color = variant.Color,
            DimensionsCm = variant.DimensionsCm,
            UnitPrice = variant.Price,
            Quantity = quantity
        };

        CartService.AddItem(HttpContext.Session, item);
        return new OkResult();
    }
}
