using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PhoneShop.ViewModels;

public class SurveyAnswerViewModel
{
    public int SurveyId { get; set; }

    [BindNever]
    [ValidateNever]
    public string Title { get; set; } = string.Empty;

    [BindNever]
    [ValidateNever]
    public string Question { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập câu trả lời.")]
    [StringLength(2000,
        ErrorMessage = "Câu trả lời tối đa 2.000 ký tự.")]
    [Display(Name = "Câu trả lời của bạn")]
    public string Answer { get; set; } = string.Empty;
}