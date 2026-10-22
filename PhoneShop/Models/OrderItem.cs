using System.ComponentModel.DataAnnotations;

namespace PhoneShop.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    [Range(1, 99)]
    public int Quantity { get; set; }
}