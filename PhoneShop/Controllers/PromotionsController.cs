using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneShop.Data;

namespace PhoneShop.Controllers;

[AllowAnonymous]
public class PromotionsController : Controller
{
    private readonly AppDbContext _context;

    public PromotionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;

        var promotions = await _context.Promotions
            .AsNoTracking()
            .Where(p =>
                p.IsActive &&
                p.StartsAtUtc <= now &&
                p.EndsAtUtc > now)
            .OrderBy(p => p.EndsAtUtc)
            .ToListAsync();

        return View(promotions);
    }
}