using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneShop.Data;
using PhoneShop.Models;
using PhoneShop.ViewModels;

namespace PhoneShop.Controllers;

[Authorize(Roles = "Admin")]
public class AdminSurveysController : Controller
{
    private readonly AppDbContext _context;

    public AdminSurveysController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var surveys = await _context.Surveys
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAtUtc)
            .ThenByDescending(s => s.Id)
            .Select(s => new AdminSurveyListViewModel
            {
                Id = s.Id,
                Title = s.Title,
                IsActive = s.IsActive,
                ResponseCount = s.Responses.Count()
            })
            .ToListAsync();

        return View(surveys);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new SurveyFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SurveyFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Title) ||
            string.IsNullOrWhiteSpace(model.Question))
        {
            ModelState.AddModelError(
                string.Empty, "Tiêu đề và câu hỏi không được để trống.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var survey = new Survey
        {
            Title = model.Title.Trim(),
            Question = model.Question.Trim(),
            IsActive = model.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Surveys.Add(survey);
        await _context.SaveChangesAsync();

        TempData["SurveySuccess"] = "Đã tạo khảo sát.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool isActive)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var survey = await _context.Surveys.FindAsync(id);

        if (survey == null)
            return NotFound();

        survey.IsActive = isActive;
        await _context.SaveChangesAsync();

        TempData["SurveySuccess"] = isActive
            ? "Đã mở khảo sát."
            : "Đã đóng khảo sát.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Results(int id)
    {
        var survey = await _context.Surveys
            .AsNoTracking()
            .Include(s => s.Responses)
            .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (survey == null)
            return NotFound();

        return View(survey);
    }
}