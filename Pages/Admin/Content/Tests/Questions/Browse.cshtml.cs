using GridAcademy.Common;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Content.Masters;
using GridAcademy.DTOs.Content.Questions;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Content.Tests.Questions;

[Authorize(Roles = "Admin,Instructor")]
public class BrowseModel : PageModel
{
    private readonly IQuestionService _questions;
    private readonly ITestService     _tests;
    private readonly IMasterService   _masters;

    public BrowseModel(IQuestionService questions, ITestService tests, IMasterService masters)
    {
        _questions = questions;
        _tests     = tests;
        _masters   = masters;
    }

    // ── Route / filters ───────────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)] public Guid           TestId            { get; set; }
    [BindProperty(SupportsGet = true)] public string?        Search            { get; set; }
    [BindProperty(SupportsGet = true)] public int?           SubjectId         { get; set; }
    [BindProperty(SupportsGet = true)] public int?           TopicId           { get; set; }
    [BindProperty(SupportsGet = true)] public int?           DifficultyLevelId { get; set; }
    [BindProperty(SupportsGet = true)] public QuestionType?  QuestionType      { get; set; }
    [BindProperty(SupportsGet = true)] public int            CurrentPage       { get; set; } = 1;
    public const int PageSize = 25;

    // ── Results ───────────────────────────────────────────────────────────────
    public string                     TestTitle        { get; set; } = "";
    public PagedResult<QuestionDto>   Questions        { get; set; } = new();
    public HashSet<Guid>              MappedIds        { get; set; } = [];
    public List<SubjectDto>           Subjects         { get; set; } = [];
    public List<TopicDto>             Topics           { get; set; } = [];
    public List<DifficultyLevelDto>   DifficultyLevels { get; set; } = [];

    // ── POST binding ──────────────────────────────────────────────────────────
    [BindProperty] public List<Guid> SelectedIds { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        // Validate test exists
        try
        {
            var test = await _tests.GetTestByIdAsync(TestId);
            TestTitle = test.Title;
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Test not found.";
            return RedirectToPage("/Admin/Content/Tests/Index");
        }

        await LoadPageDataAsync();
        return Page();
    }

    /// <summary>Add selected questions to the test and redirect back to Manage Questions.</summary>
    public async Task<IActionResult> OnPostAddAsync()
    {
        if (SelectedIds.Count == 0)
        {
            TempData["Error"] = "Please select at least one question to add.";
            await LoadTestTitleAsync();
            await LoadPageDataAsync();
            return Page();
        }

        try
        {
            await _tests.AddQuestionsToTestAsync(TestId, SelectedIds);
            TempData["Success"] = $"{SelectedIds.Count} question(s) added to the test.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not add questions: {ex.Message}";
        }

        return RedirectToPage("Index", new { testId = TestId });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task LoadTestTitleAsync()
    {
        try
        {
            var test = await _tests.GetTestByIdAsync(TestId);
            TestTitle = test.Title;
        }
        catch { /* ignore */ }
    }

    private async Task LoadPageDataAsync()
    {
        // Load published questions from global bank (not filtered to this test)
        var filter = new QuestionListRequest
        {
            Search            = Search,
            SubjectId         = SubjectId,
            TopicId           = TopicId,
            DifficultyLevelId = DifficultyLevelId,
            QuestionType      = QuestionType,
            Status            = QuestionStatus.Published,
            Page              = CurrentPage,
            PageSize          = PageSize
        };
        Questions = await _questions.GetQuestionsAsync(filter);

        // Get already-mapped question IDs so we can show them as checked/disabled
        var mapped = await _tests.GetTestQuestionsAsync(TestId);
        MappedIds  = mapped.Select(q => q.QuestionId).ToHashSet();

        // Dropdowns
        Subjects         = await _masters.GetSubjectsAsync();
        Topics           = SubjectId.HasValue
                           ? await _masters.GetTopicsAsync(SubjectId)
                           : await _masters.GetTopicsAsync();
        DifficultyLevels = await _masters.GetDifficultyLevelsAsync();
    }
}
