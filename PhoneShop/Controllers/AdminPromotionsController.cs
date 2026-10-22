using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneShop.Data;
using PhoneShop.Models;
using PhoneShop.ViewModels;

namespace PhoneShop.Controllers;

[Authorize(Roles = "Admin")]
public class AdminPromotionsController : Controller
{
    private readonly AppDbContext _context;

    public AdminPromotionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var promotions = await _context.Promotions
            .AsNoTracking()
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return View(promotions);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View("Edit", new PromotionViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create(PromotionViewModel model)
    {
        return SavePromotion(model, creating: true);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var promotion = await _context.Promotions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (promotion == null)
            return NotFound();

        return View(new PromotionViewModel
        {
            Id = promotion.Id,
            Code = promotion.Code,
            Name = promotion.Name,
            Description = promotion.Description,
            DiscountPercent = promotion.DiscountPercent,
            MinimumOrderAmount = promotion.MinimumOrderAmount,
            MaximumDiscountAmount = promotion.MaximumDiscountAmount,
            StartsAt = promotion.StartsAtUtc.AddHours(7),
            EndsAt = promotion.EndsAtUtc.AddHours(7),
            IsActive = promotion.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(PromotionViewModel model)
    {
        return SavePromotion(model, creating: false);
    }

    private async Task<IActionResult> SavePromotion(
        PromotionViewModel model, bool creating)
    {
        if (creating)
            model.Id = 0;

        if (!ModelState.IsValid)
            return View("Edit", model);

        var code = model.Code.Trim().ToUpperInvariant();

        var duplicate = await _context.Promotions
            .AnyAsync(p => p.Code == code && p.Id != model.Id);

        if (duplicate)
        {
            ModelState.AddModelError(
                nameof(model.Code), "Mã khuyến mãi đã tồn tại.");

            return View("Edit", model);
        }

        Promotion promotion;

        if (creating)
        {
            promotion = new Promotion();
            _context.Promotions.Add(promotion);
        }
        else
        {
            var existing = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (existing == null)
                return NotFound();

            promotion = existing;
        }

        promotion.Code = code;
        promotion.Name = model.Name.Trim();
        promotion.Description = model.Description?.Trim() ?? "";
        promotion.DiscountPercent = model.DiscountPercent;
        promotion.MinimumOrderAmount = model.MinimumOrderAmount;
        promotion.MaximumDiscountAmount = model.MaximumDiscountAmount;
        promotion.IsActive = model.IsActive;

        // Form dùng giờ Việt Nam, database lưu UTC.
        promotion.StartsAtUtc = DateTime.SpecifyKind(
            model.StartsAt, DateTimeKind.Unspecified).AddHours(-7);

        promotion.EndsAtUtc = DateTime.SpecifyKind(
            model.EndsAt, DateTimeKind.Unspecified).AddHours(-7);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627))
        {
            ModelState.AddModelError(
                nameof(model.Code), "Mã khuyến mãi đã tồn tại.");

            return View("Edit", model);
        }

        TempData["PromotionSuccess"] = "Đã lưu khuyến mãi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool isActive)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var promotion = await _context.Promotions.FindAsync(id);

        if (promotion == null)
            return NotFound();

        promotion.IsActive = isActive;
        await _context.SaveChangesAsync();

        TempData["PromotionSuccess"] =
            isActive ? "Đã bật khuyến mãi." : "Đã tắt khuyến mãi.";

        return RedirectToAction(nameof(Index));
    }
}