using DogBed.Models;
using Newtonsoft.Json;

namespace DogBed.Services;

public static class CartService
{
    private const string CartKey = "Cart";

    public static Cart GetCart(ISession session)
    {
        var json = session.GetString(CartKey);
        return json == null ? new Cart() : JsonConvert.DeserializeObject<Cart>(json) ?? new Cart();
    }

    public static void SaveCart(ISession session, Cart cart)
    {
        session.SetString(CartKey, JsonConvert.SerializeObject(cart));
    }

    public static Cart AddItem(ISession session, CartItem item)
    {
        var cart = GetCart(session);
        var existing = cart.Items.FirstOrDefault(i =>
            i.ProductId == item.ProductId && i.Size == item.Size && i.Color == item.Color);

        if (existing != null)
            existing.Quantity += item.Quantity;
        else
            cart.Items.Add(item);

        SaveCart(session, cart);
        return cart;
    }

    public static Cart UpdateQuantity(ISession session, int productId, string size, string color, int quantity)
    {
        var cart = GetCart(session);
        var item = cart.Items.FirstOrDefault(i =>
            i.ProductId == productId && i.Size == size && i.Color == color);

        if (item != null)
        {
            if (quantity <= 0)
                cart.Items.Remove(item);
            else
                item.Quantity = quantity;
        }

        SaveCart(session, cart);
        return cart;
    }

    public static void ClearCart(ISession session)
    {
        session.Remove(CartKey);
    }
}
