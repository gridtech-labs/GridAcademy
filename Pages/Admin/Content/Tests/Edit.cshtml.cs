using System.Security.Claims;
using GridAcademy.Data.Entities.Assessment;
using GridAcademy.DTOs.Assessment;
using GridAcademy.DTOs.Content.Masters;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Content.Tests;

[Authorize(Roles = "Admin,Instructor,Provider")]
public class EditModel : PageModel
{
    private readonly ITestService   _tests;
    private readonly IMasterService _masters;

    public EditModel(ITestService tests, IMasterService masters)
    {
        _tests   = tests;
        _masters = masters;
    }

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }

    public TestDetailDto? Test { get; set; }

    [BindProperty] public UpdateTestRequest    TestForm    { get; set; } = new();
    [BindProperty] public CreateTestSectionRequest SectionForm { get; set; } = new();

    public List<ExamTypeDto>        ExamTypes        { get; set; } = [];
    public List<SubjectDto>         Subjects         { get; set; } = [];
    public List<DifficultyLevelDto> DifficultyLevels { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadDropdownsAsync();
        try
        {
            Test = await _tests.GetTestByIdAsync(Id);
            // Pre-populate the test form for editing
            TestForm = new UpdateTestRequest
            {
                Title                  = Test.Title,
                Instructions           = Test.Instructions,
                DurationMinutes        = Test.DurationMinutes,
                PassingPercent         = Test.PassingPercent,
                NegativeMarkingEnabled = Test.NegativeMarkingEnabled,
                ExamTypeId             = Test.ExamTypeId,
                QuestionMode           = Test.QuestionMode
            };
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Test not found.";
            return RedirectToPage("/Admin/Content/Tests/Index");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostTestAsync()
    {
        await LoadDropdownsAsync();
        Test = await _tests.GetTestByIdAsync(Id);

        if (!ModelState.IsValid)
            return Page();

        var userId = GetUserId();
        try
        {
            await _tests.UpdateTestAsync(Id, TestForm, userId);
            TempData["Success"] = "Test details saved.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to save: {ex.Message}";
        }
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddSectionAsync()
    {
        var userId = GetUserId();
        try
        {
            await _tests.AddSectionAsync(Id, SectionForm);
            TempData["Success"] = "Section added.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to add section: {ex.Message}";
        }
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteSectionAsync(int sectionId)
    {
        try
        {
            await _tests.DeleteSectionAsync(Id, sectionId);
            TempData["Success"] = "Section removed.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to remove section: {ex.Message}";
        }
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostPublishAsync()
    {
        try
        {
            await _tests.PublishTestAsync(Id);
            TempData["Success"] = "Test published successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not publish: {ex.Message}";
        }
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostUnpublishAsync()
    {
        try
        {
            await _tests.UnpublishTestAsync(Id);
            TempData["Success"] = "Test unpublished.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not unpublish: {ex.Message}";
        }
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostSetModeAsync(QuestionMode mode)
    {
        try
        {
            await _tests.SetQuestionModeAsync(Id, mode);
            TempData["Success"] = $"Question mode set to {(mode == QuestionMode.Manual ? "Manual" : "Global Bank")}.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to change mode: {ex.Message}";
        }
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnGetSectionPoolAsync(int sectionId)
    {
        try
        {
            var count = await _tests.GetSectionPoolCountAsync(sectionId);
            return new JsonResult(new { count });
        }
        catch
        {
            return new JsonResult(new { count = -1 });
        }
    }

    // ── Manual question management ────────────────────────────────────────

    /// <summary>AJAX: returns all directly-mapped questions for this test.</summary>
    public async Task<IActionResult> OnGetTestQuestionsAsync()
    {
        try
        {
            var questions = await _tests.GetTestQuestionsAsync(Id);
            return new JsonResult(questions);
        }
        catch
        {
            return new JsonResult(new List<object>());
        }
    }

    /// <summary>AJAX: browse/search the question bank for the picker modal.</summary>
    public async Task<IActionResult> OnGetBrowseQuestionsAsync(
        int? subjectId, int? difficultyLevelId, string? search)
    {
        try
        {
            var items = await _tests.BrowseQuestionsForTestAsync(Id, subjectId, difficultyLevelId, search);
            return new JsonResult(items);
        }
        catch
        {
            return new JsonResult(new List<object>());
        }
    }

    /// <summary>POST: add selected question IDs to the test.</summary>
    public async Task<IActionResult> OnPostAddQuestionsAsync([FromForm] string questionIds)
    {
        try
        {
            var ids = (questionIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();

            await _tests.AddQuestionsToTestAsync(Id, ids);
            TempData["Success"] = $"{ids.Count} question(s) added to the test.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to add questions: {ex.Message}";
        }
        return RedirectToPage(new { id = Id });
    }

    /// <summary>POST: remove a single question from the test's direct mapping.</summary>
    public async Task<IActionResult> OnPostRemoveQuestionAsync(Guid questionId)
    {
        try
        {
            await _tests.RemoveQuestionFromTestAsync(Id, questionId);
            TempData["Success"] = "Question removed.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to remove question: {ex.Message}";
        }
        return RedirectToPage(new { id = Id });
    }

    private async Task LoadDropdownsAsync()
    {
        ExamTypes        = await _masters.GetExamTypesAsync();
        Subjects         = await _masters.GetSubjectsAsync();
        DifficultyLevels = await _masters.GetDifficultyLevelsAsync();
    }

    private Guid GetUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
}
