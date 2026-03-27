using System.Security.Claims;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Content.Masters;
using GridAcademy.DTOs.Content.Questions;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Content.Questions;

[Authorize(Roles = "Admin,Instructor")]
public class CreateModel : PageModel
{
    private readonly IQuestionService   _questions;
    private readonly IMasterService     _masters;
    private readonly IWebHostEnvironment _env;

    public CreateModel(IQuestionService questions, IMasterService masters, IWebHostEnvironment env)
    {
        _questions = questions;
        _masters   = masters;
        _env       = env;
    }

    // ── Edit mode ─────────────────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    public bool IsEdit => Id.HasValue;

    // ── Form ──────────────────────────────────────────────────────────────────
    [BindProperty] public QuestionFormModel Form { get; set; } = new();

    // ── Dropdowns ────────────────────────────────────────────────────────────
    public List<SubjectDto>         Subjects         { get; set; } = [];
    public List<TopicDto>           Topics           { get; set; } = [];
    public List<DifficultyLevelDto> DifficultyLevels { get; set; } = [];
    public List<ComplexityLevelDto> ComplexityLevels { get; set; } = [];
    public List<ExamTypeDto>        ExamTypes        { get; set; } = [];
    public List<MarksDto>           Marks            { get; set; } = [];
    public List<NegativeMarksDto>   NegativeMarks    { get; set; } = [];
    public List<TagDto>             Tags             { get; set; } = [];

    // ── Form model ────────────────────────────────────────────────────────────
    public class QuestionFormModel
    {
        public string  Text             { get; set; } = "";
        public string? Solution         { get; set; }
        public string? Subtopic         { get; set; }
        public QuestionType QuestionType { get; set; } = QuestionType.MCQ;
        public int SubjectId            { get; set; }
        public int TopicId              { get; set; }
        public int DifficultyLevelId    { get; set; }
        public int ComplexityLevelId    { get; set; }
        public int MarksId              { get; set; }
        public int NegativeMarksId      { get; set; }
        public int ExamTypeId           { get; set; }
        public decimal? NumericalAnswer { get; set; }
        public QuestionStatus Status    { get; set; } = QuestionStatus.Draft;

        // Fixed 4 options (A-D) — nullable: empty options are simply omitted
        public string? OptionA     { get; set; }
        public string? OptionB     { get; set; }
        public string? OptionC     { get; set; }
        public string? OptionD     { get; set; }
        public bool    CorrectA    { get; set; }
        public bool    CorrectB    { get; set; }
        public bool    CorrectC    { get; set; }
        public bool    CorrectD    { get; set; }

        // Comma-separated tag IDs (handled via hidden input + JS) — nullable: tags are optional
        public string? SelectedTagIds { get; set; }
    }

    // ── GET ───────────────────────────────────────────────────────────────────
    public async Task OnGetAsync()
    {
        await LoadDropdownsAsync();

        if (IsEdit && Id.HasValue)
        {
            try
            {
                var q = await _questions.GetByIdAsync(Id.Value);
                Form = new QuestionFormModel
                {
                    Text             = q.Text,
                    Solution         = q.Solution,
                    Subtopic         = q.Subtopic,
                    QuestionType     = q.QuestionType,
                    SubjectId        = q.SubjectId,
                    TopicId          = q.TopicId,
                    DifficultyLevelId = q.DifficultyLevelId,
                    ComplexityLevelId = q.ComplexityLevelId,
                    MarksId          = q.MarksId,
                    NegativeMarksId  = q.NegativeMarksId,
                    ExamTypeId       = q.ExamTypeId,
                    NumericalAnswer  = q.NumericalAnswer,
                    Status           = q.Status,
                    OptionA          = q.Options.FirstOrDefault(o => o.Label == 'A')?.Text ?? "",
                    OptionB          = q.Options.FirstOrDefault(o => o.Label == 'B')?.Text ?? "",
                    OptionC          = q.Options.FirstOrDefault(o => o.Label == 'C')?.Text ?? "",
                    OptionD          = q.Options.FirstOrDefault(o => o.Label == 'D')?.Text ?? "",
                    CorrectA         = q.Options.FirstOrDefault(o => o.Label == 'A')?.IsCorrect ?? false,
                    CorrectB         = q.Options.FirstOrDefault(o => o.Label == 'B')?.IsCorrect ?? false,
                    CorrectC         = q.Options.FirstOrDefault(o => o.Label == 'C')?.IsCorrect ?? false,
                    CorrectD         = q.Options.FirstOrDefault(o => o.Label == 'D')?.IsCorrect ?? false,
                    SelectedTagIds   = string.Join(",", q.Tags.Select(t => t.Id))
                };
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Question not found.";
                Response.Redirect("/Admin/Content/Questions/Index");
            }
        }
    }

    // ── POST ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAsync()
    {
        // Options and tags are optional — remove any implicit-Required errors for them
        ModelState.Remove("Form.OptionA");
        ModelState.Remove("Form.OptionB");
        ModelState.Remove("Form.OptionC");
        ModelState.Remove("Form.OptionD");
        ModelState.Remove("Form.SelectedTagIds");

        // Manual validation: question text is required
        if (string.IsNullOrWhiteSpace(Form.Text))
            ModelState.AddModelError("Form.Text", "Question text is required.");

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        // Parse tag IDs from comma-separated string
        var tagIds = string.IsNullOrWhiteSpace(Form.SelectedTagIds)
            ? []
            : Form.SelectedTagIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var n) ? n : (int?)null)
                .Where(x => x.HasValue).Select(x => x!.Value).ToList();

        // Build options list (only for MCQ types)
        var options = new List<CreateOptionRequest>();
        if (Form.QuestionType != QuestionType.NAT)
        {
            if (!string.IsNullOrWhiteSpace(Form.OptionA)) options.Add(new CreateOptionRequest { Label = 'A', Text = Form.OptionA, IsCorrect = Form.CorrectA });
            if (!string.IsNullOrWhiteSpace(Form.OptionB)) options.Add(new CreateOptionRequest { Label = 'B', Text = Form.OptionB, IsCorrect = Form.CorrectB });
            if (!string.IsNullOrWhiteSpace(Form.OptionC)) options.Add(new CreateOptionRequest { Label = 'C', Text = Form.OptionC, IsCorrect = Form.CorrectC });
            if (!string.IsNullOrWhiteSpace(Form.OptionD)) options.Add(new CreateOptionRequest { Label = 'D', Text = Form.OptionD, IsCorrect = Form.CorrectD });
        }

        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;

        try
        {
            if (IsEdit && Id.HasValue)
            {
                var req = new UpdateQuestionRequest
                {
                    Text             = Form.Text,
                    Solution         = Form.Solution,
                    Subtopic         = Form.Subtopic,
                    QuestionType     = Form.QuestionType,
                    SubjectId        = Form.SubjectId,
                    TopicId          = Form.TopicId,
                    DifficultyLevelId = Form.DifficultyLevelId,
                    ComplexityLevelId = Form.ComplexityLevelId,
                    MarksId          = Form.MarksId,
                    NegativeMarksId  = Form.NegativeMarksId,
                    ExamTypeId       = Form.ExamTypeId,
                    NumericalAnswer  = Form.NumericalAnswer,
                    Status           = Form.Status,
                    Options          = options,
                    TagIds           = tagIds
                };
                await _questions.UpdateAsync(Id.Value, req, userId);
                TempData["Success"] = "Question updated.";
            }
            else
            {
                var req = new CreateQuestionRequest
                {
                    Text             = Form.Text,
                    Solution         = Form.Solution,
                    Subtopic         = Form.Subtopic,
                    QuestionType     = Form.QuestionType,
                    SubjectId        = Form.SubjectId,
                    TopicId          = Form.TopicId,
                    DifficultyLevelId = Form.DifficultyLevelId,
                    ComplexityLevelId = Form.ComplexityLevelId,
                    MarksId          = Form.MarksId,
                    NegativeMarksId  = Form.NegativeMarksId,
                    ExamTypeId       = Form.ExamTypeId,
                    NumericalAnswer  = Form.NumericalAnswer,
                    Options          = options,
                    TagIds           = tagIds
                };
                await _questions.CreateAsync(req, userId);
                TempData["Success"] = "Question created.";
            }

            return RedirectToPage("/Admin/Content/Questions/Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadDropdownsAsync();
            return Page();
        }
    }

    // ── Image Upload (called from Quill editor via fetch ?handler=UploadImage) ──
    public async Task<IActionResult> OnPostUploadImageAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return new JsonResult(new { error = "No file received." }) { StatusCode = 400 };

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return new JsonResult(new { error = "Only JPEG, PNG, GIF and WebP images are accepted." }) { StatusCode = 400 };

        if (file.Length > 5 * 1024 * 1024)
            return new JsonResult(new { error = "Image must be under 5 MB." }) { StatusCode = 400 };

        // Build a safe filename: GUID + original extension (fallback to .jpg)
        var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        var fileName = $"{Guid.NewGuid()}{ext}";

        var dir = Path.Combine(_env.WebRootPath, "uploads", "questions");
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, fileName);
        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);

        return new JsonResult(new { url = $"/uploads/questions/{fileName}" });
    }

    private async Task LoadDropdownsAsync()
    {
        Subjects         = await _masters.GetSubjectsAsync();
        Topics           = await _masters.GetTopicsAsync();
        DifficultyLevels = await _masters.GetDifficultyLevelsAsync();
        ComplexityLevels = await _masters.GetComplexityLevelsAsync();
        ExamTypes        = await _masters.GetExamTypesAsync();
        Marks            = await _masters.GetMarksAsync();
        NegativeMarks    = await _masters.GetNegativeMarksAsync();
        Tags             = await _masters.GetTagsAsync();
    }
}
