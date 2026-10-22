namespace PhoneShop.ViewModels;

public class AdminSurveyListViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int ResponseCount { get; set; }
}