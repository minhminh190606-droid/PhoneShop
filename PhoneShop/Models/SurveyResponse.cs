using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PhoneShop.Models;

[Index(nameof(SurveyId), nameof(UserId), IsUnique = true)]
public class SurveyResponse
{
    public int Id { get; set; }

    public int SurveyId { get; set; }

    public Survey Survey { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Required]
    [StringLength(2000)]
    public string Answer { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}