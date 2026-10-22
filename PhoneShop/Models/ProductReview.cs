using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PhoneShop.Models;

[Index(nameof(UserId), nameof(ProductId), IsUnique = true)]
public class ProductReview
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5.")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nhận xét.")]
    [StringLength(2000,
        ErrorMessage = "Nhận xét tối đa 2.000 ký tự.")]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}