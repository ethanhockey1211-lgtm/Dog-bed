using DogBed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages.Api;

public class CartCountModel : PageModel
{
    public IActionResult OnGet()
    {
        var cart = CartService.GetCart(HttpContext.Session);
        return new JsonResult(new { count = cart.ItemCount });
    }
}
