using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using PhoneShop.Models;

namespace PhoneShop.ViewModels;

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
    [StringLength(100)]
    [Display(Name = "Người nhận")]
    public string ReceiverName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(20)]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
    [StringLength(300)]
    [Display(Name = "Địa chỉ nhận hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
    [EnumDataType(typeof(PaymentMethod))]
    [Display(Name = "Phương thức thanh toán")]
    public PaymentMethod? PaymentMethod { get; set; }

    [StringLength(30, ErrorMessage = "Mã khuyến mãi tối đa 30 ký tự.")]
    [Display(Name = "Mã khuyến mãi")]
    public string? PromotionCode { get; set; }

    [BindNever]
    [ValidateNever]
    public List<CartItem> Items { get; set; } = new();

    [BindNever]
    [ValidateNever]
    public decimal DiscountAmount { get; set; }

    public decimal Subtotal =>
        Items.Sum(i => i.Product.Price * i.Quantity);

    public decimal TotalAmount => Subtotal - DiscountAmount;
}