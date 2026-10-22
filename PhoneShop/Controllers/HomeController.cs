using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneShop.Data;
using PhoneShop.Models;
using System.Diagnostics;
using System.Security.Claims;
using PhoneShop.ViewModels;

namespace PhoneShop.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
     string? keyword,
     string? brand,
     decimal? minPrice,
     decimal? maxPrice,
     int? ramGb,
     int? storageGb)
    {
        keyword = keyword?.Trim();

        var productsQuery = _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive);

        ViewBag.Brands = await productsQuery
            .Select(p => p.Brand)
            .Distinct()
            .OrderBy(b => b)
            .ToListAsync();

        if (!ModelState.IsValid ||
            minPrice < 0 ||
            maxPrice < 0 ||
            (minPrice.HasValue && maxPrice.HasValue
                && minPrice.Value > maxPrice.Value) ||
            ramGb <= 0 ||
            storageGb <= 0)
        {
            ViewData["FilterError"] =
                "Bộ lọc không hợp lệ. Giá phải không âm, "
                + "giá từ không lớn hơn giá đến; "
                + "RAM và bộ nhớ phải lớn hơn 0.";

            return View(new List<Product>());
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            productsQuery = productsQuery.Where(p =>
                p.Name.Contains(keyword) ||
                p.Brand.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(brand))
            productsQuery = productsQuery.Where(p => p.Brand == brand);

        if (minPrice.HasValue)
            productsQuery = productsQuery.Where(p =>
                p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            productsQuery = productsQuery.Where(p =>
                p.Price <= maxPrice.Value);

        if (ramGb.HasValue)
            productsQuery = productsQuery.Where(p =>
                p.RamGb == ramGb.Value);

        if (storageGb.HasValue)
            productsQuery = productsQuery.Where(p =>
                p.StorageGb == storageGb.Value);

        return View(await productsQuery
            .OrderBy(p => p.Id)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (product == null)
            return NotFound();

        var model = new ProductDetailsViewModel
        {
            Product = product,

            Reviews = await _context.ProductReviews
                .AsNoTracking()
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ThenByDescending(r => r.Id)
                .Select(r => new ProductReviewDisplay
                {
                    FullName = r.User.FullName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAtUtc = r.CreatedAtUtc
                })
                .ToListAsync()
        };

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId != null)
        {
            model.HasReviewed = await _context.ProductReviews
                .AnyAsync(r =>
                    r.UserId == userId && r.ProductId == id);

            model.CanReview = !model.HasReviewed &&
                await _context.Orders.AnyAsync(o =>
                    o.UserId == userId &&
                    o.Status == OrderStatus.Delivered &&
                    o.Items.Any(i => i.ProductId == id));
        }

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0,
        Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id
                ?? HttpContext.TraceIdentifier
        });
    }
}