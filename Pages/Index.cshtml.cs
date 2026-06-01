using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages;

public class IndexModel : PageModel
{
    private readonly ProductService _productService;

    public IndexModel(ProductService productService)
    {
        _productService = productService;
    }

    public List<Product> AllProducts { get; set; } = [];

    public void OnGet()
    {
        AllProducts = _productService.GetAll();
    }
}
