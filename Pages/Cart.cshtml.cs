using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages;

public class CartModel : PageModel
{
    private readonly ProductService _products;

    public CartModel(ProductService products) => _products = products;

    public Cart Cart { get; set; } = new();
    public List<Product> CrossSells { get; set; } = [];

    public void OnGet()
    {
        Cart = CartService.GetCart(HttpContext.Session);
        var inCart = Cart.Items.Select(i => i.ProductId).ToHashSet();
        CrossSells = _products.GetAll().Where(p => !inCart.Contains(p.Id)).ToList();
    }

    // Renders the slide-out mini cart; fetched by the drawer JS in the layout
    public PartialViewResult OnGetDrawer()
    {
        Cart = CartService.GetCart(HttpContext.Session);
        return Partial("_CartDrawer", Cart);
    }

    public IActionResult OnPostUpdateCart(int productId, string size, string color, int quantity)
    {
        CartService.UpdateQuantity(HttpContext.Session, productId, size, color, quantity);
        return RedirectToPage();
    }

    public IActionResult OnPostQuickAdd(int productId)
    {
        var product = _products.GetById(productId);
        var variant = product?.Variants.FirstOrDefault();
        if (product == null || variant == null) return BadRequest();

        CartService.AddItem(HttpContext.Session, new CartItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Size = variant.Size,
            Color = variant.Color,
            DimensionsCm = variant.DimensionsCm,
            UnitPrice = variant.Price,
            OriginalUnitPrice = variant.OriginalPrice,
            Quantity = 1,
            Image = product.Images.FirstOrDefault() ?? "/images/placeholder.svg"
        });
        return RedirectToPage();
    }
}
