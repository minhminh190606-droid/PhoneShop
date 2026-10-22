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
public class SurveysController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SurveysController> _logger;

    public SurveysController(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<SurveysController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var surveys = await _context.Surveys
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ThenByDescending(s => s.Id)
            .Select(s => new CustomerSurveyViewModel
            {
                Id = s.Id,
                Title = s.Title,
                Question = s.Question,
                HasAnswered = s.Responses
                    .Any(r => r.UserId == userId)
            })
            .ToListAsync();

        return View(surveys);
    }

    [HttpGet]
    public async Task<IActionResult> Answer(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var survey = await _context.Surveys
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (survey == null)
            return NotFound();

        if (!survey.IsActive)
        {
            TempData["SurveyError"] =
                "Khảo sát này đã đóng.";

            return RedirectToAction(nameof(Index));
        }

        var hasAnswered = await _context.SurveyResponses
            .AnyAsync(r =>
                r.SurveyId == id &&
                r.UserId == userId);

        if (hasAnswered)
        {
            TempData["SurveyError"] =
                "Bạn đã trả lời khảo sát này rồi.";

            return RedirectToAction(nameof(Index));
        }

        return View(new SurveyAnswerViewModel
        {
            SurveyId = survey.Id,
            Title = survey.Title,
            Question = survey.Question
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Answer(
        SurveyAnswerViewModel model)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        model.Answer = model.Answer?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(model.Answer))
        {
            ModelState.AddModelError(
                nameof(model.Answer),
                "Vui lòng nhập câu trả lời.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var survey = await _context.Surveys
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.Id == model.SurveyId);

            if (survey == null)
                return NotFound();

            if (!survey.IsActive)
            {
                await transaction.RollbackAsync();

                TempData["SurveyError"] =
                    "Khảo sát đã đóng, không thể gửi câu trả lời.";

                return RedirectToAction(nameof(Index));
            }

            var hasAnswered = await _context.SurveyResponses
                .AnyAsync(r =>
                    r.SurveyId == survey.Id &&
                    r.UserId == userId);

            if (hasAnswered)
            {
                await transaction.RollbackAsync();

                TempData["SurveyError"] =
                    "Bạn đã trả lời khảo sát này rồi.";

                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                model.Title = survey.Title;
                model.Question = survey.Question;

                await transaction.RollbackAsync();

                return View(model);
            }

            _context.SurveyResponses.Add(new SurveyResponse
            {
                SurveyId = survey.Id,
                UserId = userId,
                Answer = model.Answer,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SurveySuccess"] =
                "Đã gửi câu trả lời. Cảm ơn bạn đã góp ý!";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627))
        {
            await transaction.RollbackAsync();

            TempData["SurveyError"] =
                "Bạn đã trả lời khảo sát này rồi.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (
            ex is DbUpdateException || ex is SqlException)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                ex,
                "Không thể lưu trả lời khảo sát {SurveyId} "
                + "của người dùng {UserId}",
                model.SurveyId,
                userId);

            TempData["SurveyError"] =
                "Chưa gửi được câu trả lời. Vui lòng thử lại.";

            return RedirectToAction(nameof(Index));
        }
    }
}