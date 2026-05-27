using DogBed.Models;
using DogBed.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogBed.Pages;

public class OrdersModel : PageModel
{
    private readonly OrderStore _store;
    public OrdersModel(OrderStore store) => _store = store;

    public IReadOnlyList<FulfillmentOrder> Orders { get; private set; } = [];

    public void OnGet() => Orders = _store.GetAll();
}
