using System.Data;
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
public class OrdersController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<OrdersController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    private async Task<List<CartItem>> GetCartAsync(string userId)
    {
        return await _context.CartItems
            .AsNoTracking()
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.ProductId)
            .ToListAsync();
    }

    private async Task<(
        string? Code,
        decimal Discount,
        string? Error)> CalculatePromotionAsync(
            decimal subtotal,
            string? inputCode)
    {
        if (string.IsNullOrWhiteSpace(inputCode))
            return (null, 0m, null);

        var code = inputCode.Trim().ToUpperInvariant();

        if (code.Length > 30)
            return (null, 0m, "Mã khuyến mãi tối đa 30 ký tự.");

        var now = DateTime.UtcNow;

        var promotion = await _context.Promotions
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Code == code &&
                p.IsActive &&
                p.StartsAtUtc <= now &&
                p.EndsAtUtc > now);

        if (promotion == null)
        {
            return (null, 0m,
                "Mã không tồn tại, chưa bắt đầu, "
                + "đã hết hạn hoặc đã tắt.");
        }

        if (subtotal < promotion.MinimumOrderAmount)
        {
            return (null, 0m,
                "Đơn hàng chưa đạt giá trị tối thiểu của mã.");
        }

        if (promotion.DiscountPercent <= 0 ||
            promotion.DiscountPercent > 100 ||
            (promotion.MaximumDiscountAmount.HasValue &&
             promotion.MaximumDiscountAmount.Value <= 0))
        {
            return (null, 0m,
                "Chương trình khuyến mãi không hợp lệ.");
        }

        var discount =
            subtotal * promotion.DiscountPercent / 100m;

        if (promotion.MaximumDiscountAmount.HasValue)
        {
            discount = Math.Min(
                discount,
                promotion.MaximumDiscountAmount.Value);
        }

        discount = decimal.Round(
            discount, 2, MidpointRounding.AwayFromZero);

        discount = Math.Min(discount, subtotal);

        return (promotion.Code, discount, null);
    }

    private async Task PrepareCheckoutAsync(
        CheckoutViewModel model,
        string userId)
    {
        model.Items = await GetCartAsync(userId);

        var promotion = await CalculatePromotionAsync(
            model.Subtotal, model.PromotionCode);

        model.DiscountAmount = promotion.Discount;

        if (promotion.Error != null)
        {
            ModelState.AddModelError(
                nameof(model.PromotionCode), promotion.Error);
        }
        else
        {
            model.PromotionCode = promotion.Code;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(string? promotionCode)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var model = new CheckoutViewModel
        {
            ReceiverName = user.FullName,
            PhoneNumber = user.PhoneNumber ?? "",
            ShippingAddress = user.Address ?? "",
            PromotionCode = promotionCode
        };

        await PrepareCheckoutAsync(model, user.Id);

        if (model.Items.Count == 0)
        {
            TempData["CartError"] = "Giỏ hàng đang trống.";
            return RedirectToAction("Index", "Cart");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(
        CheckoutViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        model.ReceiverName = model.ReceiverName?.Trim() ?? "";
        model.PhoneNumber = model.PhoneNumber?.Trim() ?? "";
        model.ShippingAddress =
            model.ShippingAddress?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(model.ReceiverName) ||
            string.IsNullOrWhiteSpace(model.PhoneNumber) ||
            string.IsNullOrWhiteSpace(model.ShippingAddress))
        {
            ModelState.AddModelError(
                string.Empty,
                "Vui lòng nhập đầy đủ thông tin nhận hàng.");
        }

        if (!ModelState.IsValid)
        {
            await PrepareCheckoutAsync(model, user.Id);
            return View(model);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var cart = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .OrderBy(c => c.ProductId)
                .ToListAsync();

            if (cart.Count == 0)
            {
                await transaction.RollbackAsync();

                TempData["CartError"] =
                    "Giỏ hàng đang trống hoặc đã được đặt.";

                return RedirectToAction("Index", "Cart");
            }

            foreach (var item in cart)
            {
                if (!item.Product.IsActive ||
                    item.Quantity < 1 ||
                    item.Quantity > 99 ||
                    item.Quantity > item.Product.StockQuantity ||
                    item.Product.Price <= 0)
                {
                    await transaction.RollbackAsync();

                    TempData["CartError"] =
                        $"Vui lòng kiểm tra lại sản phẩm "
                        + $"'{item.Product.Name}' và tồn kho.";

                    return RedirectToAction("Index", "Cart");
                }
            }

            var subtotal = cart.Sum(
                i => i.Product.Price * i.Quantity);

            // Kiểm tra lại mã ở thời điểm tạo đơn.
            var promotion = await CalculatePromotionAsync(
                subtotal, model.PromotionCode);

            if (promotion.Error != null)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    nameof(model.PromotionCode), promotion.Error);

                model.Items = cart;
                model.DiscountAmount = 0;

                return View(model);
            }

            var order = new Order
            {
                UserId = user.Id,
                ReceiverName = model.ReceiverName,
                PhoneNumber = model.PhoneNumber,
                ShippingAddress = model.ShippingAddress,
                PaymentMethod = model.PaymentMethod!.Value,
                PaymentStatus = PaymentStatus.Unpaid,
                Status = OrderStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,

                Subtotal = subtotal,
                PromotionCode = promotion.Code,
                DiscountAmount = promotion.Discount,
                TotalAmount = subtotal - promotion.Discount
            };

            foreach (var item in cart)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    UnitPrice = item.Product.Price,
                    Quantity = item.Quantity
                });

                item.Product.StockQuantity -= item.Quantity;
            }

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cart);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(
                nameof(Details), new { id = order.Id });
        }
        catch (Exception ex) when (
            ex is DbUpdateException || ex is SqlException)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                ex,
                "Không thể tạo đơn cho người dùng {UserId}",
                user.Id);

            TempData["CartError"] =
                "Chưa tạo được đơn. Vui lòng kiểm tra giỏ và thử lại.";

            return RedirectToAction("Index", "Cart");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o =>
                o.Id == id && o.UserId == userId);

        if (order == null)
            return NotFound();

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ThenByDescending(o => o.Id)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o =>
                    o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            if (order.Status != OrderStatus.Pending ||
                order.PaymentStatus != PaymentStatus.Unpaid)
            {
                await transaction.RollbackAsync();

                TempData["OrderError"] =
                    "Chỉ được hủy đơn đang chờ xác nhận "
                    + "và chưa thanh toán.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            foreach (var item in order.Items.OrderBy(i => i.ProductId))
            {
                item.Product.StockQuantity += item.Quantity;
            }

            order.Status = OrderStatus.Cancelled;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["OrderSuccess"] =
                "Đã hủy đơn hàng và hoàn lại tồn kho.";

            return RedirectToAction(
                nameof(Details), new { id });
        }
        catch (Exception ex) when (
            ex is DbUpdateException || ex is SqlException)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                ex, "Không thể hủy đơn hàng {OrderId}", id);

            TempData["OrderError"] =
                "Chưa hủy được đơn. Vui lòng tải lại trang và thử lại.";

            return RedirectToAction(
                nameof(Details), new { id });
        }
    }
}