using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneShop.Data;
using System.Data;
using Microsoft.Data.SqlClient;
using PhoneShop.Models;
using System.Security.Claims;
using PhoneShop.ViewModels;

namespace PhoneShop.Controllers;

[Authorize(Roles = "Admin")]
public class AdminOrdersController : Controller
{
    private readonly AppDbContext _context;

    public AdminOrdersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAtUtc)
            .ThenByDescending(o => o.Id)
            .ToListAsync();

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return View(order);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
    int id,
    OrderStatus nextStatus,
    bool paymentCollected = false)
    {
        if (!ModelState.IsValid ||
            !Enum.IsDefined(typeof(OrderStatus), nextStatus))
        {
            return BadRequest();
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var validTransition =
                (order.Status == OrderStatus.Pending &&
                 nextStatus == OrderStatus.Confirmed) ||

                (order.Status == OrderStatus.Confirmed &&
                 nextStatus == OrderStatus.Shipping) ||

                (order.Status == OrderStatus.Shipping &&
                 nextStatus == OrderStatus.Delivered);

            if (!validTransition)
            {
                await transaction.RollbackAsync();

                TempData["AdminError"] =
                    "Trạng thái đơn đã thay đổi hoặc không được "
                    + "chuyển theo thứ tự này.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (order.PaymentMethod == PaymentMethod.BankTransfer &&
                nextStatus == OrderStatus.Shipping &&
                order.PaymentStatus != PaymentStatus.Paid)
            {
                await transaction.RollbackAsync();

                TempData["AdminError"] =
                    "Đơn chuyển khoản phải được xác nhận "
                    + "thanh toán trước khi giao.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (nextStatus == OrderStatus.Delivered)
            {
                if (order.PaymentMethod == PaymentMethod.CashOnDelivery &&
                    order.PaymentStatus == PaymentStatus.Unpaid)
                {
                    if (!paymentCollected)
                    {
                        await transaction.RollbackAsync();

                        TempData["AdminError"] =
                            "Cần xác nhận đã thu đủ tiền khi nhận hàng.";

                        return RedirectToAction(
                            nameof(Details), new { id });
                    }

                    order.PaymentStatus = PaymentStatus.Paid;
                }

                if (order.PaymentStatus != PaymentStatus.Paid)
                {
                    await transaction.RollbackAsync();

                    TempData["AdminError"] =
                        "Chỉ hoàn thành đơn khi đã thanh toán.";

                    return RedirectToAction(
                        nameof(Details), new { id });
                }
            }

            order.Status = nextStatus;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["AdminSuccess"] = "Đã cập nhật trạng thái đơn.";

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex) when (
            ex is DbUpdateException || ex is SqlException)
        {
            await transaction.RollbackAsync();

            TempData["AdminError"] =
                "Chưa cập nhật được. Vui lòng tải lại trang và thử lại.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(
    ConfirmPaymentViewModel model)
    {
        if (!ModelState.IsValid ||
            !model.Verified ||
            string.IsNullOrWhiteSpace(model.Reference))
        {
            TempData["AdminError"] =
                "Nhập mã giao dịch, số tiền và xác nhận "
                + "đã đối chiếu ngân hàng.";

            return RedirectToAction(
                nameof(Details), new { id = model.OrderId });
        }

        var adminId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (adminId == null)
            return Challenge();

        var reference = model.Reference.Trim();

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == model.OrderId);

            if (order == null)
                return NotFound();

            if (order.PaymentMethod != PaymentMethod.BankTransfer ||
                order.PaymentStatus != PaymentStatus.Unpaid ||
                (order.Status != OrderStatus.Pending &&
                 order.Status != OrderStatus.Confirmed))
            {
                await transaction.RollbackAsync();

                TempData["AdminError"] =
                    "Đơn không đủ điều kiện xác nhận chuyển khoản.";

                return RedirectToAction(
                    nameof(Details), new { id = order.Id });
            }

            if (model.Amount != order.TotalAmount)
            {
                await transaction.RollbackAsync();

                TempData["AdminError"] =
                    "Số tiền giao dịch phải khớp tổng tiền đơn hàng.";

                return RedirectToAction(
                    nameof(Details), new { id = order.Id });
            }

            var referenceUsed = await _context.Orders
                .AnyAsync(o => o.PaymentReference == reference);

            if (referenceUsed)
            {
                await transaction.RollbackAsync();

                TempData["AdminError"] =
                    "Mã giao dịch này đã được sử dụng.";

                return RedirectToAction(
                    nameof(Details), new { id = order.Id });
            }

            order.PaymentStatus = PaymentStatus.Paid;
            order.PaidAtUtc = DateTime.UtcNow;
            order.PaymentConfirmedByUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);
            order.PaymentReference = reference;
            order.PaidAtUtc = DateTime.UtcNow;
            order.PaymentConfirmedByUserId = adminId;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["AdminSuccess"] =
                "Đã xác nhận nhận tiền chuyển khoản.";

            return RedirectToAction(
                nameof(Details), new { id = order.Id });
        }
        catch (Exception ex) when (
            ex is DbUpdateException || ex is SqlException)
        {
            await transaction.RollbackAsync();

            TempData["AdminError"] =
                "Chưa xác nhận được. Kiểm tra mã giao dịch "
                + "và tải lại trang trước khi thử lại.";

            return RedirectToAction(
                nameof(Details), new { id = model.OrderId });
        }
    }
}