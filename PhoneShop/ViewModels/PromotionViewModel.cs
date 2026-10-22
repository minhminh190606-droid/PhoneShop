using System.ComponentModel.DataAnnotations;

namespace PhoneShop.ViewModels;

public class PromotionViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi.")]
    [StringLength(30)]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$",
        ErrorMessage = "Mã chỉ gồm chữ không dấu, số, _ hoặc -.")]
    [Display(Name = "Mã khuyến mãi")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên chương trình.")]
    [StringLength(150)]
    [Display(Name = "Tên chương trình")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "100",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Phần trăm giảm phải từ 0,01 đến 100.")]
    [Display(Name = "Giảm giá (%)")]
    public decimal DiscountPercent { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Giá trị đơn tối thiểu không hợp lệ.")]
    [Display(Name = "Giá trị đơn tối thiểu (đồng)")]
    public decimal MinimumOrderAmount { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Mức giảm tối đa phải lớn hơn 0.")]
    [Display(Name = "Giảm tối đa (đồng, để trống nếu không giới hạn)")]
    public decimal? MaximumDiscountAmount { get; set; }

    [Display(Name = "Bắt đầu (giờ Việt Nam)")]
    public DateTime StartsAt { get; set; }
        = DateTime.UtcNow.AddHours(7);

    [Display(Name = "Kết thúc (giờ Việt Nam)")]
    public DateTime EndsAt { get; set; }
        = DateTime.UtcNow.AddHours(7).AddDays(7);

    [Display(Name = "Bật chương trình")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Vui lòng nhập tên chương trình.",
                new[] { nameof(Name) });
        }

        if (StartsAt == default ||
            EndsAt == default ||
            EndsAt <= StartsAt)
        {
            yield return new ValidationResult(
                "Ngày kết thúc phải sau ngày bắt đầu.",
                new[] { nameof(EndsAt) });
        }
    }
}