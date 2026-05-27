using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages;

public class CartModel : PageModel
{
    public Cart Cart { get; set; } = new();

    public void OnGet()
    {
        Cart = CartService.GetCart(HttpContext.Session);
    }

    public IActionResult OnPostUpdateCart(int productId, string size, string color, int quantity)
    {
        CartService.UpdateQuantity(HttpContext.Session, productId, size, color, quantity);
        return RedirectToPage();
    }
}
