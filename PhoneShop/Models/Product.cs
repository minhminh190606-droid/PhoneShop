using System.ComponentModel.DataAnnotations;

namespace PhoneShop.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên điện thoại.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập hãng.")]
    [StringLength(50)]
    public string Brand { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "999999999",
        ErrorMessage = "Giá phải lớn hơn 0.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue,
        ErrorMessage = "Tồn kho không được âm.")]
    public int StockQuantity { get; set; }

    [Range(1, 128)]
    public int RamGb { get; set; }

    [Range(1, 4096)]
    public int StorageGb { get; set; }

    [StringLength(100)]
    public string Chipset { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}