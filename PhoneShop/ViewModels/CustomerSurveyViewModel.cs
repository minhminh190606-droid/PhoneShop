namespace PhoneShop.ViewModels;

public class CustomerSurveyViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Question { get; set; } = string.Empty;

    public bool HasAnswered { get; set; }
}