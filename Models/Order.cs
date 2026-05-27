namespace DogBed.Models;

public class Order
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Country { get; set; } = "United States";
    public string CardNumber { get; set; } = "";
    public string CardExpiry { get; set; } = "";
    public string CardCvc { get; set; } = "";
    public string CardName { get; set; } = "";
}
