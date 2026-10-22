using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PhoneShop.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Address { get; set; }
}