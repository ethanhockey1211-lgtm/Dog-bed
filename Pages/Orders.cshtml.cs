using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages;

public class OrdersModel : PageModel
{
    private readonly OrderStore _store;
    private readonly IConfiguration _config;
    private const string AuthKey = "admin_authed";

    public OrdersModel(OrderStore store, IConfiguration config)
    {
        _store  = store;
        _config = config;
    }

    public IReadOnlyList<FulfillmentOrder> Orders { get; private set; } = [];
    public bool Authed { get; private set; }

    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetString(AuthKey) != "yes")
            return Page(); // show login form
        Authed = true;
        Orders = _store.GetAll();
        return Page();
    }

    public IActionResult OnPost(string password)
    {
        var correct = _config["Admin:Password"];
        if (!string.IsNullOrEmpty(correct) && password == correct)
        {
            HttpContext.Session.SetString(AuthKey, "yes");
            return RedirectToPage();
        }
        ModelState.AddModelError("", "Incorrect password.");
        return Page();
    }
}
