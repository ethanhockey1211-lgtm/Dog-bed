using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages;

public class CheckoutModel : PageModel
{
    [BindProperty]
    public Order Order { get; set; } = new();

    public Cart Cart { get; set; } = new();
    public bool OrderPlaced { get; set; }
    public string OrderNumber { get; set; } = "";

    public void OnGet()
    {
        Cart = CartService.GetCart(HttpContext.Session);
    }

    public IActionResult OnPost()
    {
        Cart = CartService.GetCart(HttpContext.Session);

        if (!ModelState.IsValid)
            return Page();

        OrderNumber = new Random().Next(100000, 999999).ToString();
        CartService.ClearCart(HttpContext.Session);
        OrderPlaced = true;
        return Page();
    }
}
