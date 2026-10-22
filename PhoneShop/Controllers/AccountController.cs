using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhoneShop.Models;
using PhoneShop.ViewModels;

namespace PhoneShop.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        if (!ModelState.IsValid)
            return View(model);

        var fullName = model.FullName.Trim();
        var email = model.Email.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ModelState.AddModelError(
                nameof(model.FullName), "Vui lòng nhập họ tên.");

            return View(model);
        }

        var user = new ApplicationUser
        {
            FullName = fullName,
            Email = email,
            UserName = email
        };

        var result = await _userManager.CreateAsync(
            user, model.Password);

        if (result.Succeeded)
        {
            TempData["Success"] =
                "Đăng ký thành công. Bạn có thể quay về trang chủ.";

            return RedirectToAction(nameof(Register));
        }

        foreach (var error in result.Errors)
        {
            var message = error.Code switch
            {
                "DuplicateUserName" or "DuplicateEmail" =>
                    "Email này đã được sử dụng.",

                "PasswordTooShort" =>
                    "Mật khẩu phải có ít nhất 8 ký tự.",

                "PasswordRequiresDigit" =>
                    "Mật khẩu phải có chữ số.",

                "PasswordRequiresLower" =>
                    "Mật khẩu phải có chữ thường.",

                "PasswordRequiresUpper" =>
                    "Mật khẩu phải có chữ hoa.",

                "PasswordRequiresNonAlphanumeric" =>
                    "Mật khẩu phải có ký tự đặc biệt.",

                _ => "Không thể đăng ký. Vui lòng kiểm tra thông tin."
            };

            ModelState.AddModelError(string.Empty, message);
        }

        return View(model);
    }
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(
            model.Email.Trim());

        if (user == null)
        {
            ModelState.AddModelError(
                string.Empty, "Email hoặc mật khẩu không đúng.");

            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(model.ReturnUrl)
                && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        var message = result.IsLockedOut
            ? "Tài khoản đang bị khóa. Nếu do đăng nhập sai, hãy thử lại sau 5 phút; nếu vẫn bị khóa, hãy liên hệ quản trị viên."
            : "Email hoặc mật khẩu không đúng.";

        ModelState.AddModelError(string.Empty, message);
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return Content("Bạn không có quyền truy cập chức năng này.");
    }
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        return View(new ProfileViewModel
        {
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        // Email lấy từ tài khoản đang đăng nhập.
        model.Email = user.Email;

        if (!ModelState.IsValid)
            return View(model);

        var fullName = model.FullName.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ModelState.AddModelError(
                nameof(model.FullName), "Vui lòng nhập họ tên.");

            return View(model);
        }

        user.FullName = fullName;
        user.PhoneNumber = model.PhoneNumber?.Trim();
        user.Address = model.Address?.Trim();

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu thông tin. Vui lòng kiểm tra lại.");

            return View(model);
        }

        TempData["ProfileSuccess"] = "Cập nhật thông tin thành công.";

        return RedirectToAction(nameof(Profile));
    }
}