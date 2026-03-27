using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using GridAcademy.Data.Entities.Assessment;

namespace GridAcademy.Pages.Student;

[Authorize(Roles = "User")]
public class TakeModel : PageModel
{
    private readonly IAssessmentService _svc;

    public TakeModel(IAssessmentService svc)
    {
        _svc = svc;
    }

    [BindProperty(SupportsGet = true)]
    public Guid AttemptId { get; set; }

    public AttemptStateDto? Exam { get; set; }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> OnGetAsync()
    {
        if (AttemptId == Guid.Empty)
        {
            TempData["Error"] = "Invalid attempt.";
            return RedirectToPage("/Student/Dashboard");
        }

        try
        {
            Exam = await _svc.GetAttemptStateAsync(AttemptId, CurrentUserId);
        }
        catch (Exception)
        {
            TempData["Error"] = "Unable to load exam. Please try again.";
            return RedirectToPage("/Student/Dashboard");
        }

        if (Exam.Status != AttemptStatus.InProgress)
        {
            // Submitted or timed out — go to result
            return RedirectToPage("/Student/Assessment/Result", new { attemptId = AttemptId });
        }

        ViewData["ExamMode"] = true;
        ViewData["ExamTitle"] = Exam.TestTitle;
        ViewData["ExamDurationSeconds"] = Exam.DurationSeconds - Exam.SecondsElapsed;

        return Page();
    }

    // ── AJAX Handlers (called by exam JS via fetch with antiforgery token) ──

    public async Task<IActionResult> OnPostSaveAnswerAsync([FromBody] SaveAnswerRequest request)
    {
        if (AttemptId == Guid.Empty)
            return new JsonResult(new { ok = false, error = "Invalid attempt." }) { StatusCode = 400 };

        try
        {
            await _svc.SaveAnswerAsync(AttemptId, request, CurrentUserId);
            return new JsonResult(new { ok = true });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Time expired")
        {
            return new JsonResult(new { ok = false, timedOut = true, redirectUrl = $"/Student/Assessment/Result?attemptId={AttemptId}" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnPostToggleMarkAsync(Guid questionId)
    {
        if (AttemptId == Guid.Empty || questionId == Guid.Empty)
            return new JsonResult(new { ok = false }) { StatusCode = 400 };

        try
        {
            await _svc.ToggleMarkForReviewAsync(AttemptId, questionId, CurrentUserId);
            return new JsonResult(new { ok = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnPostVisitAsync(Guid questionId)
    {
        if (AttemptId == Guid.Empty || questionId == Guid.Empty)
            return new JsonResult(new { ok = false }) { StatusCode = 400 };

        try
        {
            await _svc.MarkVisitedAsync(AttemptId, questionId, CurrentUserId);
            return new JsonResult(new { ok = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnPostViolationAsync([FromBody] ViolationRequest request)
    {
        if (AttemptId == Guid.Empty)
            return new JsonResult(new { ok = false }) { StatusCode = 400 };

        try
        {
            await _svc.LogViolationAsync(AttemptId, request.ViolationType, CurrentUserId);
            return new JsonResult(new { ok = true });
        }
        catch (Exception)
        {
            // Violations are best-effort; don't fail the client on logging errors
            return new JsonResult(new { ok = true });
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync()
    {
        if (AttemptId == Guid.Empty)
            return new JsonResult(new { ok = false, error = "Invalid attempt." }) { StatusCode = 400 };

        try
        {
            await _svc.SubmitAttemptAsync(AttemptId, CurrentUserId);
            return new JsonResult(new
            {
                ok = true,
                redirectUrl = $"/Student/Assessment/Result?attemptId={AttemptId}"
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message }) { StatusCode = 500 };
        }
    }
}
