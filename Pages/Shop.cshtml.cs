using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages;

public class ShopModel : PageModel
{
    private readonly ProductService _products;
    public ShopModel(ProductService products) => _products = products;

    public List<Product> Products { get; private set; } = [];

    public void OnGet()
    {
        Products = _products.GetAll();
    }
}
