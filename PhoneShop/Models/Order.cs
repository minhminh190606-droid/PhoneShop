using System.ComponentModel.DataAnnotations;

namespace PhoneShop.Models;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipping,
    Delivered,
    Cancelled
}

public enum PaymentMethod
{
    CashOnDelivery,
    BankTransfer
}

public enum PaymentStatus
{
    Unpaid,
    Paid,
    Refunded
}

public class Order
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(100)]
    public string ReceiverName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string ShippingAddress { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public PaymentMethod PaymentMethod { get; set; }

    public PaymentStatus PaymentStatus { get; set; }
        = PaymentStatus.Unpaid;

    public decimal TotalAmount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    [StringLength(30)]
    public string? PromotionCode { get; set; }

    [StringLength(100)]
    public string? PaymentReference { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    [StringLength(450)]
    public string? PaymentConfirmedByUserId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}