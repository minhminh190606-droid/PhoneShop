using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneShop.Data;
using PhoneShop.Models;
using PhoneShop.ViewModels;

namespace PhoneShop.Controllers;

[Authorize(Roles = "Admin")]
public class AdminCustomersController : Controller
{
    private readonly AppDbContext _context;

    public AdminCustomersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var customers = await _context.Users
            .AsNoTracking()
            .Select(user => new CustomerClassificationViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? "",

                CompletedOrderCount = _context.Orders.Count(o =>
                    o.UserId == user.Id &&
                    o.Status == OrderStatus.Delivered &&
                    o.PaymentStatus == PaymentStatus.Paid),

                TotalSpent = _context.Orders
                    .Where(o =>
                        o.UserId == user.Id &&
                        o.Status == OrderStatus.Delivered &&
                        o.PaymentStatus == PaymentStatus.Paid)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0m
            })
            .OrderByDescending(c => c.TotalSpent)
            .ThenBy(c => c.Email)
            .ToListAsync();

        return View(customers);
    }
}