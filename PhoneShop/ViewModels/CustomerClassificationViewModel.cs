namespace PhoneShop.ViewModels;

public class CustomerClassificationViewModel
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int CompletedOrderCount { get; set; }

    public decimal TotalSpent { get; set; }

    public string Tier => TotalSpent switch
    {
        >= 50000000m => "Vàng",
        >= 20000000m => "Bạc",
        _ => "Đồng"
    };
}