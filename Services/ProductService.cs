using DogBed.Models;

namespace DogBed.Services;

public class ProductService
{
    private readonly List<Product> _products;

    public ProductService()
    {
        _products = new List<Product>
        {
            new Product
            {
                Id = 1,
                Name = "PET HEAVEN Super Sized Dog Bed",
                ShortDescription = "Soft, dirt-resistant & non-slip — the ultimate comfort bed for dogs of all sizes.",
                Description = "Give your dog the sleep they deserve. The PET HEAVEN bed features a deep bolster design with ultra-soft corduroy velvet lining, a waterproof non-slip base, and a removable washable cover. Perfect for living rooms, bedrooms, and outdoor spaces.",
                Rating = 4.9,
                ReviewCount = 43,
                Features = new List<string>
                {
                    "Ultra-soft corduroy velvet lining",
                    "Dirt & water resistant exterior",
                    "Non-slip rubber bottom",
                    "Removable & machine washable cover",
                    "Deep bolster walls for security & warmth",
                    "Suitable for indoors & outdoors",
                    "Available in 4 sizes for all breeds"
                },
                Images = new List<string>
                {
                    "/images/bed-hero.jpg",
                    "/images/bed-corgi.jpg",
                    "/images/bed-golden.jpg",
                    "/images/bed-wash.jpg",
                    "/images/bed-product.jpg"
                },
                Variants = new List<ProductVariant>
                {
                    new ProductVariant { Size = "S", DimensionsCm = "47×35×17 cm", DimensionsInch = "18.5×13.8×6.7 in", Price = 24.45m, OriginalPrice = 97.78m, Color = "Gray", Stock = 50 },
                    new ProductVariant { Size = "M", DimensionsCm = "60×50×20 cm", DimensionsInch = "23.6×19.7×7.9 in", Price = 32.38m, OriginalPrice = 121.48m, Color = "Gray", Stock = 40 },
                    new ProductVariant { Size = "L", DimensionsCm = "80×60×23 cm", DimensionsInch = "31.5×23.6×9.1 in", Price = 42.10m, OriginalPrice = 145.78m, Color = "Gray", Stock = 35 },
                    new ProductVariant { Size = "XL", DimensionsCm = "100×80×26 cm", DimensionsInch = "39.4×31.5×10.2 in", Price = 53.44m, OriginalPrice = 178.18m, Color = "Gray", Stock = 20 },
                },
                Reviews = new List<Review>
                {
                    new Review { Author = "Sarah M.", Stars = 5, Comment = "My golden retriever absolutely loves this bed! It's so plush and the bolster sides make him feel totally secure. Easy to wash too.", Date = "May 12, 2025", VerifiedPurchase = "XL - Gray" },
                    new Review { Author = "James T.", Stars = 5, Comment = "Bought the L size for my lab mix. Excellent quality for the price. The non-slip bottom is a huge bonus on our hardwood floors.", Date = "April 28, 2025", VerifiedPurchase = "L - Gray" },
                    new Review { Author = "Priya K.", Stars = 5, Comment = "Got the M for my corgi — perfect fit! The fabric is incredibly soft and it washed beautifully in the machine. 10/10.", Date = "April 3, 2025", VerifiedPurchase = "M - Gray" },
                    new Review { Author = "Mike R.", Stars = 4, Comment = "Really nice bed, my dogs fight over it now. Docking one star because delivery took a bit longer than expected, but the product itself is great.", Date = "March 19, 2025", VerifiedPurchase = "XL - Gray" },
                }
            }
        };
    }

    public Product? GetById(int id) => _products.FirstOrDefault(p => p.Id == id);

    public List<Product> GetAll() => _products;
}
