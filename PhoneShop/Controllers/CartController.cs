using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneShop.Data;
using PhoneShop.Models;

namespace PhoneShop.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CartController(
        AppDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var items = await _context.CartItems
            .AsNoTracking()
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Id)
            .ToListAsync();

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null || !product.IsActive)
        {
            TempData["CartError"] = "Sản phẩm không còn được bán.";
            return RedirectToAction(nameof(Index));
        }

        var item = await _context.CartItems
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.ProductId == productId);

        var newQuantity = (item?.Quantity ?? 0) + 1;

        if (newQuantity > 99 ||
            newQuantity > product.StockQuantity)
        {
            TempData["CartError"] =
                "Không thể thêm: vượt tồn kho hoặc giới hạn 99 sản phẩm.";

            return RedirectToAction(nameof(Index));
        }

        if (item == null)
        {
            _context.CartItems.Add(new CartItem
            {
                UserId = userId,
                ProductId = productId,
                Quantity = 1
            });
        }
        else
        {
            item.Quantity = newQuantity;
        }

        await _context.SaveChangesAsync();

        TempData["CartSuccess"] = "Đã thêm sản phẩm vào giỏ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int quantity)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        if (!ModelState.IsValid || quantity < 1 || quantity > 99)
        {
            TempData["CartError"] = "Số lượng phải từ 1 đến 99.";
            return RedirectToAction(nameof(Index));
        }

        var item = await _context.CartItems
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c =>
                c.Id == id && c.UserId == userId);

        if (item == null)
            return NotFound();

        if (!item.Product.IsActive ||
            quantity > item.Product.StockQuantity)
        {
            TempData["CartError"] =
                "Sản phẩm ngừng bán hoặc số lượng vượt tồn kho.";

            return RedirectToAction(nameof(Index));
        }

        item.Quantity = quantity;
        await _context.SaveChangesAsync();

        TempData["CartSuccess"] = "Đã cập nhật số lượng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var item = await _context.CartItems
            .FirstOrDefaultAsync(c =>
                c.Id == id && c.UserId == userId);

        if (item == null)
            return NotFound();

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();

        TempData["CartSuccess"] = "Đã xóa sản phẩm khỏi giỏ.";
        return RedirectToAction(nameof(Index));
    }
}