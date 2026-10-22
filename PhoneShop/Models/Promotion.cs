using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PhoneShop.Models;

[Index(nameof(Code), IsUnique = true)]
public class Promotion
{
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100")]
    public decimal DiscountPercent { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal MinimumOrderAmount { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal? MaximumDiscountAmount { get; set; }

    public DateTime StartsAtUtc { get; set; }

    public DateTime EndsAtUtc { get; set; }

    public bool IsActive { get; set; } = true;
}