using GridAcademy.Common;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Assessment;
using GridAcademy.DTOs.Content.Masters;
using GridAcademy.DTOs.Content.Questions;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Content.Tests.Questions;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel : PageModel
{
    private readonly IQuestionService _questions;
    private readonly ITestService     _tests;
    private readonly IMasterService   _masters;

    public IndexModel(IQuestionService questions, ITestService tests, IMasterService masters)
    {
        _questions = questions;
        _tests     = tests;
        _masters   = masters;
    }

    // ── Route / filter ────────────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)] public Guid          TestId            { get; set; }
    [BindProperty(SupportsGet = true)] public string?       Search            { get; set; }
    [BindProperty(SupportsGet = true)] public int?          SubjectId         { get; set; }
    [BindProperty(SupportsGet = true)] public int?          TopicId           { get; set; }
    [BindProperty(SupportsGet = true)] public int?          DifficultyLevelId { get; set; }
    [BindProperty(SupportsGet = true)] public QuestionType? QuestionType      { get; set; }
    [BindProperty(SupportsGet = true)] public QuestionStatus? Status          { get; set; }
    [BindProperty(SupportsGet = true)] public int           CurrentPage       { get; set; } = 1;
    public const int PageSize = 20;

    // ── Results ───────────────────────────────────────────────────────────────
    public TestDetailDto?            Test             { get; set; }
    public PagedResult<QuestionDto>  Questions        { get; set; } = new();
    public List<SubjectDto>          Subjects         { get; set; } = [];
    public List<TopicDto>            Topics           { get; set; } = [];
    public List<DifficultyLevelDto>  DifficultyLevels { get; set; } = [];

    // ── Section add form binding ──────────────────────────────────────────────
    [BindProperty] public CreateTestSectionRequest SectionForm { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Test = await _tests.GetTestByIdAsync(TestId);
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Test not found.";
            return RedirectToPage("/Admin/Content/Tests/Index");
        }

        await LoadPageDataAsync();
        return Page();
    }

    // ── Section management ────────────────────────────────────────────────────

    public async Task<IActionResult> OnPostAddSectionAsync()
    {
        try
        {
            await _tests.AddSectionAsync(TestId, SectionForm);
            TempData["Success"] = $"Section \"{SectionForm.Name}\" added.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not add section: {ex.Message}";
        }
        return RedirectToPage(new { testId = TestId });
    }

    public async Task<IActionResult> OnPostDeleteSectionAsync(int sectionId)
    {
        try
        {
            await _tests.DeleteSectionAsync(TestId, sectionId);
            TempData["Success"] = "Section removed.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not remove section: {ex.Message}";
        }
        return RedirectToPage(new { testId = TestId });
    }

    public async Task<IActionResult> OnPostAssignSectionAsync(Guid questionId, int? sectionId)
    {
        try
        {
            await _tests.AssignQuestionToSectionAsync(TestId, questionId, sectionId == 0 ? null : sectionId);
            TempData["Success"] = "Section assigned.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not assign section: {ex.Message}";
        }
        return RedirectToPage(new { testId = TestId });
    }

    // ── Question actions ──────────────────────────────────────────────────────

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        try
        {
            await _questions.PublishAsync(id);
            TempData["Success"] = "Question published.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not publish: {ex.Message}";
        }
        return RedirectToPage(new { testId = TestId });
    }

    public async Task<IActionResult> OnPostUnpublishAsync(Guid id)
    {
        try
        {
            await _questions.UnpublishAsync(id);
            TempData["Success"] = "Question moved back to draft.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not unpublish: {ex.Message}";
        }
        return RedirectToPage(new { testId = TestId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _tests.RemoveQuestionFromTestAsync(TestId, id);
            await _questions.DeleteAsync(id);
            TempData["Success"] = "Question deleted and removed from test.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not delete: {ex.Message}";
        }
        return RedirectToPage(new { testId = TestId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid id)
    {
        try
        {
            await _tests.RemoveQuestionFromTestAsync(TestId, id);
            TempData["Success"] = "Question removed from test.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not remove: {ex.Message}";
        }
        return RedirectToPage(new { testId = TestId });
    }

    // ── AJAX helpers ──────────────────────────────────────────────────────────

    /// <summary>Returns question→section mappings as JSON so the JS can pre-select dropdowns.</summary>
    public async Task<IActionResult> OnGetTestQuestionsAsync()
    {
        try
        {
            var list = await _tests.GetTestQuestionsAsync(TestId);
            return new JsonResult(list.Select(q => new { q.QuestionId, q.SectionId, q.SectionName }));
        }
        catch
        {
            return new JsonResult(Array.Empty<object>());
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task LoadPageDataAsync()
    {
        var filter = new QuestionListRequest
        {
            TestId            = TestId,
            Search            = Search,
            SubjectId         = SubjectId,
            TopicId           = TopicId,
            DifficultyLevelId = DifficultyLevelId,
            QuestionType      = QuestionType,
            Status            = Status,
            Page              = CurrentPage,
            PageSize          = PageSize
        };

        Questions        = await _questions.GetQuestionsAsync(filter);
        Subjects         = await _masters.GetSubjectsAsync();
        Topics           = SubjectId.HasValue
                           ? await _masters.GetTopicsAsync(SubjectId)
                           : await _masters.GetTopicsAsync();
        DifficultyLevels = await _masters.GetDifficultyLevelsAsync();
    }
}
