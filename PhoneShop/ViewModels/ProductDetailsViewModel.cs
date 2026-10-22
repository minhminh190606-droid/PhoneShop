using PhoneShop.Models;

namespace PhoneShop.ViewModels;

public class ProductDetailsViewModel
{
    public Product Product { get; set; } = null!;

    public List<ProductReviewDisplay> Reviews { get; set; } = new();

    public bool CanReview { get; set; }

    public bool HasReviewed { get; set; }
}

public class ProductReviewDisplay
{
    public string FullName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}