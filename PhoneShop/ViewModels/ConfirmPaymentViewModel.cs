using System.ComponentModel.DataAnnotations;

namespace PhoneShop.ViewModels;

public class ConfirmPaymentViewModel
{
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã giao dịch.")]
    [StringLength(100)]
    public string Reference { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "9999999999999999",
        ErrorMessage = "Số tiền không hợp lệ.")]
    public decimal Amount { get; set; }

    public bool Verified { get; set; }
}