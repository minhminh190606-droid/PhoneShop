using System.ComponentModel.DataAnnotations;

namespace PhoneShop.ViewModels;

public class ReviewInputModel
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 5, ErrorMessage = "Vui lòng chọn từ 1 đến 5 sao.")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nhận xét.")]
    [StringLength(2000)]
    public string Comment { get; set; } = string.Empty;
}