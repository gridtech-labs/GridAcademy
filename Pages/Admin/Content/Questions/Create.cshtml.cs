using System.Security.Claims;
using System.Text.Json;
using GridAcademy.Helpers;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Content.Masters;
using GridAcademy.DTOs.Content.Questions;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Content.Questions;

[Authorize(Roles = "Admin,Instructor")]
public class CreateModel : PageModel
{
    private readonly IQuestionService    _questions;
    private readonly IMasterService      _masters;
    private readonly IWebHostEnvironment _env;

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

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
        // ── Shared ────────────────────────────────────────────────────────────
        public string       Text         { get; set; } = "";
        public string?      Solution     { get; set; }
        public string?      Subtopic     { get; set; }
        public QuestionType QuestionType { get; set; } = QuestionType.MCQ;
        public int          SubjectId    { get; set; }
        public int          TopicId      { get; set; }
        public int          DifficultyLevelId { get; set; }
        public int          ComplexityLevelId { get; set; }
        public int          MarksId       { get; set; }
        public int          NegativeMarksId   { get; set; }
        public int          ExamTypeId    { get; set; }
        public QuestionStatus Status      { get; set; } = QuestionStatus.Draft;
        public string?      SelectedTagIds    { get; set; }

        // ── MCQ / MSQ: fixed options A–D ──────────────────────────────────────
        public string? OptionA  { get; set; }
        public string? OptionB  { get; set; }
        public string? OptionC  { get; set; }
        public string? OptionD  { get; set; }
        public bool    CorrectA { get; set; }
        public bool    CorrectB { get; set; }
        public bool    CorrectC { get; set; }
        public bool    CorrectD { get; set; }

        // ── NAT ───────────────────────────────────────────────────────────────
        public decimal? NumericalAnswer    { get; set; }
        public decimal? NumericalTolerance { get; set; }

        // ── True / False ──────────────────────────────────────────────────────
        /// <summary>true → "True" is the correct answer; false → "False" is correct.</summary>
        public bool TfTrueIsCorrect { get; set; } = true;

        // ── Fill in Blanks — JSON produced by JS ──────────────────────────────
        public string? BlanksJson { get; set; }

        // ── Match (MTF / MMQ) — JSON produced by JS ───────────────────────────
        public string? MatchJson { get; set; }

        // ── Assertion & Reason ────────────────────────────────────────────────
        public string? AssertionText  { get; set; }
        public string? ReasonText     { get; set; }
        /// <summary>1–4: which of the four standard A&R options is correct.</summary>
        public int ArCorrectOption { get; set; } = 1;

        // ── Passage Based ─────────────────────────────────────────────────────
        public Guid?   PassageId          { get; set; }    // edit mode: existing passage
        public string? PassageTitle       { get; set; }
        public string? PassageTextContent { get; set; }
        /// <summary>JSON array of sub-question objects produced by JS.</summary>
        public string? SubQuestionsJson   { get; set; }
    }

    // ── GET ───────────────────────────────────────────────────────────────────
    public async Task OnGetAsync()
    {
        await LoadDropdownsAsync();

        if (!IsEdit || !Id.HasValue) return;

        try
        {
            var q = await _questions.GetByIdAsync(Id.Value);
            Form = new QuestionFormModel
            {
                Text              = q.Text,
                Solution          = q.Solution,
                Subtopic          = q.Subtopic,
                QuestionType      = q.QuestionType,
                SubjectId         = q.SubjectId,
                TopicId           = q.TopicId,
                DifficultyLevelId = q.DifficultyLevelId,
                ComplexityLevelId = q.ComplexityLevelId,
                MarksId           = q.MarksId,
                NegativeMarksId   = q.NegativeMarksId,
                ExamTypeId        = q.ExamTypeId,
                NumericalAnswer   = q.NumericalAnswer,
                NumericalTolerance= q.NumericalTolerance,
                Status            = q.Status,
                AssertionText     = q.AssertionText,
                ReasonText        = q.ReasonText,
                SelectedTagIds    = string.Join(",", q.Tags.Select(t => t.Id)),

                // MCQ / MSQ
                OptionA  = q.Options.FirstOrDefault(o => o.Label == 'A')?.Text ?? "",
                OptionB  = q.Options.FirstOrDefault(o => o.Label == 'B')?.Text ?? "",
                OptionC  = q.Options.FirstOrDefault(o => o.Label == 'C')?.Text ?? "",
                OptionD  = q.Options.FirstOrDefault(o => o.Label == 'D')?.Text ?? "",
                CorrectA = q.Options.FirstOrDefault(o => o.Label == 'A')?.IsCorrect ?? false,
                CorrectB = q.Options.FirstOrDefault(o => o.Label == 'B')?.IsCorrect ?? false,
                CorrectC = q.Options.FirstOrDefault(o => o.Label == 'C')?.IsCorrect ?? false,
                CorrectD = q.Options.FirstOrDefault(o => o.Label == 'D')?.IsCorrect ?? false,

                // True/False
                TfTrueIsCorrect = q.Options.FirstOrDefault(o => o.Label == 'T')?.IsCorrect ?? true,

                // AssertionReason: find which option (1–4) is marked correct
                ArCorrectOption = q.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => int.TryParse(o.Label.ToString(), out var n) ? n : 0)
                    .FirstOrDefault(n => n >= 1 && n <= 4).Let(n => n == 0 ? 1 : n),

                // FillInBlanks
                BlanksJson = q.Blanks.Count > 0
                    ? JsonSerializer.Serialize(q.Blanks.OrderBy(b => b.BlankIndex)
                        .Select(b => new
                        {
                            blankIndex       = b.BlankIndex,
                            correctAnswer    = b.CorrectAnswer,
                            alternateAnswers = b.AlternateAnswers ?? "",
                            caseSensitive    = b.CaseSensitive
                        }))
                    : null,

                // Match (MTF / MMQ)
                MatchJson = q.MatchItems.Count > 0
                    ? JsonSerializer.Serialize(new
                      {
                          items   = q.MatchItems.OrderBy(i => i.SortOrder)
                                      .Select(i => new { side = i.ColumnSide, label = i.Label, text = i.Text }),
                          correct = q.MatchCorrect
                                      .Select(c => new { left = c.LeftLabel, right = c.RightLabel })
                      })
                    : null,

                // Passage Based
                PassageId          = q.Passage?.Id,
                PassageTitle       = q.Passage?.Title,
                PassageTextContent = q.Passage?.PassageText
            };
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Question not found.";
            Response.Redirect("/Admin/Content/Questions/Index");
        }
    }

    // ── POST ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAsync()
    {
        // Clear model-state errors for fields populated by JS / not applicable to all types
        foreach (var key in new[]
        {
            "Form.OptionA","Form.OptionB","Form.OptionC","Form.OptionD",
            "Form.SelectedTagIds","Form.BlanksJson","Form.MatchJson",
            "Form.AssertionText","Form.ReasonText",
            "Form.PassageTextContent","Form.SubQuestionsJson"
        })
            ModelState.Remove(key);

        // Text is not required for PassageBased (sub-questions carry their own text)
        bool isPbq = Form.QuestionType == QuestionType.PassageBased;
        if (!isPbq && string.IsNullOrWhiteSpace(Form.Text))
            ModelState.AddModelError("Form.Text", "Question text is required.");

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        var tagIds = ParseTagIds(Form.SelectedTagIds);
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid)
                     ? uid : (Guid?)null;

        try
        {
            if (IsEdit && Id.HasValue)
            {
                await _questions.UpdateAsync(Id.Value, BuildUpdateRequest(tagIds), userId);
                TempData["Success"] = "Question updated.";
            }
            else
            {
                await _questions.CreateAsync(BuildCreateRequest(tagIds), userId);
                TempData["Success"] = isPbq
                    ? "Passage-based question set created successfully."
                    : "Question created successfully.";
            }
            return RedirectToPage("/Admin/Content/Questions/Index");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadDropdownsAsync();
            return Page();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadDropdownsAsync();
            return Page();
        }
    }

    // ── Image Upload ──────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUploadImageAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return new JsonResult(new { error = "No file received." }) { StatusCode = 400 };

        var allowed = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowed.Contains(file.ContentType.ToLowerInvariant()))
            return new JsonResult(new { error = "Only JPEG, PNG, GIF and WebP images are accepted." }) { StatusCode = 400 };

        if (file.Length > 5 * 1024 * 1024)
            return new JsonResult(new { error = "Image must be under 5 MB." }) { StatusCode = 400 };

        var url = await UploadHelper.SaveAsync(file, _env, "questions",
            [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"]);
        return new JsonResult(new { url });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private CreateQuestionRequest BuildCreateRequest(List<int> tagIds)
    {
        var req = new CreateQuestionRequest
        {
            Text              = Form.Text?.Trim() ?? "",
            Solution          = Form.Solution,
            Subtopic          = Form.Subtopic,
            QuestionType      = Form.QuestionType,
            SubjectId         = Form.SubjectId,
            TopicId           = Form.TopicId,
            DifficultyLevelId = Form.DifficultyLevelId,
            ComplexityLevelId = Form.ComplexityLevelId,
            MarksId           = Form.MarksId,
            NegativeMarksId   = Form.NegativeMarksId,
            ExamTypeId        = Form.ExamTypeId,
            TagIds            = tagIds
        };
        PopulateTypeSpecific(req);
        return req;
    }

    private UpdateQuestionRequest BuildUpdateRequest(List<int> tagIds)
    {
        var req = new UpdateQuestionRequest
        {
            Text              = Form.Text?.Trim() ?? "",
            Solution          = Form.Solution,
            Subtopic          = Form.Subtopic,
            QuestionType      = Form.QuestionType,
            SubjectId         = Form.SubjectId,
            TopicId           = Form.TopicId,
            DifficultyLevelId = Form.DifficultyLevelId,
            ComplexityLevelId = Form.ComplexityLevelId,
            MarksId           = Form.MarksId,
            NegativeMarksId   = Form.NegativeMarksId,
            ExamTypeId        = Form.ExamTypeId,
            Status            = Form.Status,
            TagIds            = tagIds
        };
        PopulateTypeSpecific(req);
        return req;
    }

    private void PopulateTypeSpecific(CreateQuestionRequest req)
    {
        switch (Form.QuestionType)
        {
            case QuestionType.MCQ:
            case QuestionType.MSQ:
                if (!string.IsNullOrWhiteSpace(Form.OptionA)) req.Options.Add(new() { Label = 'A', Text = Form.OptionA, IsCorrect = Form.CorrectA });
                if (!string.IsNullOrWhiteSpace(Form.OptionB)) req.Options.Add(new() { Label = 'B', Text = Form.OptionB, IsCorrect = Form.CorrectB });
                if (!string.IsNullOrWhiteSpace(Form.OptionC)) req.Options.Add(new() { Label = 'C', Text = Form.OptionC, IsCorrect = Form.CorrectC });
                if (!string.IsNullOrWhiteSpace(Form.OptionD)) req.Options.Add(new() { Label = 'D', Text = Form.OptionD, IsCorrect = Form.CorrectD });
                break;

            case QuestionType.NAT:
                req.NumericalAnswer    = Form.NumericalAnswer;
                req.NumericalTolerance = Form.NumericalTolerance;
                break;

            case QuestionType.TrueFalse:
                req.Options.Add(new() { Label = 'T', Text = "True",  IsCorrect =  Form.TfTrueIsCorrect });
                req.Options.Add(new() { Label = 'F', Text = "False", IsCorrect = !Form.TfTrueIsCorrect });
                break;

            case QuestionType.FillInBlanks:
                if (!string.IsNullOrWhiteSpace(Form.BlanksJson))
                {
                    var blanks = JsonSerializer.Deserialize<List<BlankFormDto>>(Form.BlanksJson, _jsonOpts) ?? [];
                    foreach (var b in blanks.Where(b => !string.IsNullOrWhiteSpace(b.CorrectAnswer)))
                        req.Blanks.Add(new()
                        {
                            BlankIndex       = b.BlankIndex,
                            CorrectAnswer    = b.CorrectAnswer,
                            AlternateAnswers = string.IsNullOrWhiteSpace(b.AlternateAnswers) ? null : b.AlternateAnswers,
                            CaseSensitive    = b.CaseSensitive
                        });
                }
                break;

            case QuestionType.MatchTheFollowing:
            case QuestionType.MatrixMatch:
                if (!string.IsNullOrWhiteSpace(Form.MatchJson))
                {
                    var match = JsonSerializer.Deserialize<MatchFormDto>(Form.MatchJson, _jsonOpts);
                    if (match != null)
                    {
                        int s = 0;
                        foreach (var item in match.Items ?? [])
                            req.MatchItems.Add(new() { ColumnSide = item.Side, Label = item.Label, Text = item.Text, SortOrder = s++ });
                        foreach (var mc in match.Correct ?? [])
                            req.MatchCorrect.Add(new() { LeftLabel = mc.Left, RightLabel = mc.Right });
                    }
                }
                break;

            case QuestionType.AssertionReason:
                req.AssertionText = Form.AssertionText;
                req.ReasonText    = Form.ReasonText;
                // Build 4 standard options; mark the user-selected one as correct
                string[] arTexts =
                [
                    "Both Assertion (A) and Reason (R) are true and R is the correct explanation of A.",
                    "Both Assertion (A) and Reason (R) are true but R is NOT the correct explanation of A.",
                    "Assertion (A) is true but Reason (R) is false.",
                    "Assertion (A) is false but Reason (R) is true."
                ];
                for (int i = 0; i < 4; i++)
                    req.Options.Add(new() { Label = (char)('1' + i), Text = arTexts[i], IsCorrect = Form.ArCorrectOption == i + 1 });
                break;

            case QuestionType.PassageBased:
                req.PassageId    = Form.PassageId;
                req.PassageTitle = Form.PassageTitle;
                req.PassageText  = Form.PassageTextContent;
                if (!string.IsNullOrWhiteSpace(Form.SubQuestionsJson))
                {
                    var sqs = JsonSerializer.Deserialize<List<SubQFormDto>>(Form.SubQuestionsJson, _jsonOpts) ?? [];
                    foreach (var sq in sqs.Where(sq => !string.IsNullOrWhiteSpace(sq.Text)))
                    {
                        var sqReq = new CreateSubQuestionRequest
                        {
                            Text        = sq.Text,
                            Solution    = sq.Solution,
                            QuestionType= sq.QuestionType
                        };
                        foreach (var opt in sq.Options ?? [])
                            if (!string.IsNullOrWhiteSpace(opt.Text))
                                sqReq.Options.Add(new() { Label = opt.Label[0], Text = opt.Text, IsCorrect = opt.IsCorrect });
                        req.SubQuestions.Add(sqReq);
                    }
                }
                break;
        }
    }

    private static List<int> ParseTagIds(string? csv) =>
        string.IsNullOrWhiteSpace(csv) ? [] :
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
           .Select(x => int.TryParse(x.Trim(), out var n) ? n : (int?)null)
           .Where(x => x.HasValue).Select(x => x!.Value).ToList();

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

    // ── Internal JSON helper DTOs ─────────────────────────────────────────────

    private sealed class BlankFormDto
    {
        public int     BlankIndex       { get; set; }
        public string  CorrectAnswer    { get; set; } = "";
        public string? AlternateAnswers { get; set; }
        public bool    CaseSensitive    { get; set; }
    }

    private sealed class MatchItemDto  { public string Side { get; set; } = ""; public string Label { get; set; } = ""; public string Text { get; set; } = ""; }
    private sealed class MatchPairDto  { public string Left { get; set; } = ""; public string Right { get; set; } = ""; }
    private sealed class MatchFormDto  { public List<MatchItemDto>? Items { get; set; } public List<MatchPairDto>? Correct { get; set; } }

    private sealed class SubQOptDto    { public string Label { get; set; } = "A"; public string Text { get; set; } = ""; public bool IsCorrect { get; set; } }
    private sealed class SubQFormDto
    {
        public string              Text         { get; set; } = "";
        public string?             Solution     { get; set; }
        public QuestionType        QuestionType { get; set; } = QuestionType.MCQ;
        public List<SubQOptDto>?   Options      { get; set; }
    }
}

// ── Extension helper ──────────────────────────────────────────────────────────
internal static class ObjectExtensions
{
    internal static TResult Let<T, TResult>(this T self, Func<T, TResult> fn) => fn(self);
}
