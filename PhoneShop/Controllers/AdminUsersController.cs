using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneShop.Data;
using PhoneShop.Models;
using PhoneShop.ViewModels;

namespace PhoneShop.Controllers;

[Authorize(Roles = "Admin")]
public class AdminUsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;

    public AdminUsersController(
        UserManager<ApplicationUser> userManager,
        AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var model = new List<AdminUserViewModel>();

        foreach (var user in users)
        {
            model.Add(new AdminUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                IsAdmin = await _userManager.IsInRoleAsync(
                    user, "Admin"),
                IsLocked = await _userManager.IsLockedOutAsync(user)
            });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLock(
        string id, bool locked)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(id))
            return BadRequest();

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        if (user.Id == _userManager.GetUserId(User) ||
            await _userManager.IsInRoleAsync(user, "Admin"))
        {
            TempData["UserError"] =
                "Không được khóa tài khoản quản trị.";

            return RedirectToAction(nameof(Index));
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // Bật hỗ trợ khóa và xóa số lần đăng nhập sai.
            user.LockoutEnabled = true;
            user.AccessFailedCount = 0;

            // Khóa đến khi Admin mở lại.
            user.LockoutEnd = locked
                ? DateTimeOffset.UtcNow.AddYears(100)
                : null;

            var update = await _userManager.UpdateAsync(user);

            if (!update.Succeeded)
            {
                await transaction.RollbackAsync();

                TempData["UserError"] =
                    "Chưa cập nhật được tài khoản. Hãy tải lại trang.";

                return RedirectToAction(nameof(Index));
            }

            // Thu hồi các phiên đăng nhập cũ.
            var stamp = await _userManager.UpdateSecurityStampAsync(
                user);

            if (!stamp.Succeeded)
            {
                await transaction.RollbackAsync();

                TempData["UserError"] =
                    "Chưa hoàn tất thay đổi. Vui lòng thử lại.";

                return RedirectToAction(nameof(Index));
            }

            await transaction.CommitAsync();

            TempData["UserSuccess"] = locked
                ? "Đã khóa tài khoản."
                : "Đã mở khóa tài khoản.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            TempData["UserError"] =
                "Dữ liệu tài khoản vừa thay đổi. "
                + "Vui lòng tải lại trang và thử lại.";

            return RedirectToAction(nameof(Index));
        }
    }
}