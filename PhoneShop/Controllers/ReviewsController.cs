using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneShop.Data;
using PhoneShop.Models;
using PhoneShop.ViewModels;

namespace PhoneShop.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewsController(
        AppDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewInputModel model)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var productExists = await _context.Products
            .AnyAsync(p =>
                p.Id == model.ProductId && p.IsActive);

        if (!productExists)
            return NotFound();

        var hasPurchased = await _context.Orders
            .AnyAsync(o =>
                o.UserId == userId &&
                o.Status == OrderStatus.Delivered &&
                o.Items.Any(i => i.ProductId == model.ProductId));

        if (!hasPurchased)
        {
            TempData["ReviewError"] =
                "Bạn chỉ được đánh giá sản phẩm trong đơn đã giao.";

            return BackToProduct(model.ProductId);
        }

        var alreadyReviewed = await _context.ProductReviews
            .AnyAsync(r =>
                r.UserId == userId &&
                r.ProductId == model.ProductId);

        if (alreadyReviewed)
        {
            TempData["ReviewError"] =
                "Bạn đã đánh giá sản phẩm này.";

            return BackToProduct(model.ProductId);
        }

        if (!ModelState.IsValid ||
            string.IsNullOrWhiteSpace(model.Comment))
        {
            TempData["ReviewError"] =
                "Chọn từ 1 đến 5 sao và nhập nhận xét "
                + "không quá 2.000 ký tự.";

            return BackToProduct(model.ProductId);
        }

        _context.ProductReviews.Add(new ProductReview
        {
            UserId = userId,
            ProductId = model.ProductId,
            Rating = model.Rating,
            Comment = model.Comment.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });

        try
        {
            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] =
                "Đã gửi đánh giá thành công.";
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627))
        {
            TempData["ReviewError"] =
                "Bạn đã đánh giá sản phẩm này.";
        }

        return BackToProduct(model.ProductId);
    }

    private IActionResult BackToProduct(int id)
    {
        return RedirectToAction(
            "Details", "Home", new { id });
    }
}