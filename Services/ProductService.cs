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
                    "/images/IMG_1046.jpeg",
                    "/images/IMG_1047.jpeg",
                    "/images/IMG_1048.jpeg",
                    "/images/IMG_1049.jpeg"
                },
                Variants = new List<ProductVariant>
                {
                    new ProductVariant { Size = "S", DimensionsCm = "47×35×17 cm", DimensionsInch = "18.5×13.8×6.7 in", Price = 35.00m, OriginalPrice = 120.72m, Color = "Gray", Stock = 50 },
                    new ProductVariant { Size = "M", DimensionsCm = "60×50×20 cm", DimensionsInch = "23.6×19.7×7.9 in", Price = 45.00m, OriginalPrice = 149.98m, Color = "Gray", Stock = 40 },
                    new ProductVariant { Size = "L", DimensionsCm = "80×60×23 cm", DimensionsInch = "31.5×23.6×9.1 in", Price = 55.00m, OriginalPrice = 179.98m, Color = "Gray", Stock = 35 },
                    new ProductVariant { Size = "XL", DimensionsCm = "100×80×26 cm", DimensionsInch = "39.4×31.5×10.2 in", Price = 65.00m, OriginalPrice = 219.98m, Color = "Gray", Stock = 20 },
                },
                Reviews = new List<Review>
                {
                    new Review { Author = "Sarah M.", Stars = 5, Comment = "My golden retriever absolutely loves this bed! It's so plush and the bolster sides make him feel totally secure. Easy to wash too.", Date = "May 12, 2025", VerifiedPurchase = "XL - Gray" },
                    new Review { Author = "James T.", Stars = 5, Comment = "Bought the L size for my lab mix. Excellent quality for the price. The non-slip bottom is a huge bonus on our hardwood floors.", Date = "April 28, 2025", VerifiedPurchase = "L - Gray" },
                    new Review { Author = "Priya K.", Stars = 5, Comment = "Got the M for my corgi — perfect fit! The fabric is incredibly soft and it washed beautifully in the machine. 10/10.", Date = "April 3, 2025", VerifiedPurchase = "M - Gray" },
                    new Review { Author = "Mike R.", Stars = 4, Comment = "Really nice bed, my dogs fight over it now. Docking one star because delivery took a bit longer than expected, but the product itself is great.", Date = "March 19, 2025", VerifiedPurchase = "XL - Gray" },
                }
            },
            new Product
            {
                Id = 2,
                Name = "PET HEAVEN Self-Cleaning Grooming Brush",
                ShortDescription = "One-click hair removal with bent needle massaging comb — for dogs & cats of all sizes.",
                Description = "Keep your pet's coat clean and healthy with the PET HEAVEN grooming brush. The bent needle design gently massages skin while removing loose fur, and the one-click button ejects collected hair instantly. Works on all coat types.",
                Rating = 4.8,
                ReviewCount = 21,
                Features = new List<string>
                {
                    "One-click self-cleaning button",
                    "Bent needle massaging comb",
                    "Gentle on skin — works on all coat types",
                    "Ergonomic anti-slip handle",
                    "Removes loose fur, tangles & dander",
                    "Suitable for dogs & cats"
                },
                Images = new List<string>
                {
                    "/images/image0.jpeg",
                    "/images/image1.jpeg",
                    "/images/image2.jpeg",
                    "/images/image3.jpeg",
                    "/images/image4.jpeg",
                    "/images/image5.jpeg"
                },
                Variants = new List<ProductVariant>
                {
                    new ProductVariant { Size = "One Size", DimensionsCm = "19×7 cm", DimensionsInch = "7.5×2.8 in", Price = 24.99m, OriginalPrice = 49.99m, Color = "White/Gray", Stock = 100 }
                },
                Reviews = new List<Review>
                {
                    new Review { Author = "Kelly B.", Stars = 5, Comment = "My husky sheds like crazy and this brush is a lifesaver. The one-click release makes cleanup so easy!", Date = "May 8, 2025", VerifiedPurchase = "One Size - White/Gray" },
                    new Review { Author = "Tom W.", Stars = 5, Comment = "Works great on my golden retriever. Gets so much fur out and he actually enjoys the massage. Worth every penny.", Date = "April 20, 2025", VerifiedPurchase = "One Size - White/Gray" },
                    new Review { Author = "Diane L.", Stars = 4, Comment = "Really solid brush. Used it on my two cats and it works perfectly. Easy to clean too.", Date = "April 5, 2025", VerifiedPurchase = "One Size - White/Gray" },
                }
            }
        };
    }

    public Product? GetById(int id) => _products.FirstOrDefault(p => p.Id == id);

    public List<Product> GetAll() => _products;
}
