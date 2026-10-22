using System.ComponentModel.DataAnnotations;

namespace PhoneShop.ViewModels;

public class SurveyFormViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(150)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập câu hỏi.")]
    [StringLength(1000)]
    [Display(Name = "Câu hỏi")]
    public string Question { get; set; } = string.Empty;

    [Display(Name = "Mở nhận câu trả lời")]
    public bool IsActive { get; set; } = true;
}