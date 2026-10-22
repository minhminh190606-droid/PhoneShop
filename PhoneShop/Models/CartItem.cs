using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PhoneShop.Models;

[Index(nameof(UserId), nameof(ProductId), IsUnique = true)]
public class CartItem
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    [Range(1, 99,
        ErrorMessage = "Số lượng phải từ 1 đến 99.")]
    public int Quantity { get; set; }
}