using GridAcademy.Data;
using GridAcademy.Modules.AiGeneration.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.AiGeneration.Exams;

[Authorize(Roles = "SuperAdmin,Admin,Instructor")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    // ── URL params ─────────────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)] public int?  Id         { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? ExamPageId { get; set; }

    // ── View data ──────────────────────────────────────────────────────────
    public AiExamConfig?  Config     { get; private set; }
    public string         ExamTitle  { get; private set; } = "";

    public List<AiExamSection>          Sections  { get; private set; } = [];
    public List<AiExamTopic>            Topics    { get; private set; } = [];
    public GenerationPromptTemplate?    ActiveTemplate { get; private set; }

    // ── Reference data ─────────────────────────────────────────────────────
    public List<(int Id, string Name)> SubjectOptions { get; private set; } = [];
    public List<(int Id, string Name)> TopicOptions   { get; private set; } = [];

    // ── Bind section form ──────────────────────────────────────────────────
    [BindProperty] public string  SectionName         { get; set; } = "";
    [BindProperty] public int?    SectionSubjectId     { get; set; }
    [BindProperty] public int     SectionNumQuestions  { get; set; } = 25;

    // ── Bind topic form ────────────────────────────────────────────────────
    [BindProperty] public int     TopicSectionId   { get; set; }
    [BindProperty] public string  TopicName        { get; set; } = "";
    [BindProperty] public int?    TopicTopicId     { get; set; }
    [BindProperty] public string? TopicSyllabus    { get; set; }

    // ── Bind prompt form ───────────────────────────────────────────────────
    [BindProperty] public string PromptText { get; set; } = "";

    public bool TablesReady { get; private set; } = true;

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            await LoadAsync();
        }
        catch (Exception ex) when (IsSchemaNotReady(ex))
        {
            TablesReady = false;
            return Page(); // page shows "initializing" banner
        }
        return Config is null && !ExamPageId.HasValue ? NotFound() : Page();
    }

    // ── Save config notes ──────────────────────────────────────────────────
    public async Task<IActionResult> OnPostSaveConfigAsync(string? notes)
    {
        await EnsureConfigAsync();
        Config!.Notes = notes;
        Config.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Config saved.";
        return RedirectToPage(new { Id = Config.Id });
    }

    // ── Add section ────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddSectionAsync()
    {
        await EnsureConfigAsync();
        if (string.IsNullOrWhiteSpace(SectionName))
        {
            TempData["Error"] = "Section name is required.";
            return RedirectToPage(new { Id = Config!.Id });
        }

        var maxOrder = await _db.AiExamSections
            .Where(s => s.AiExamConfigId == Config!.Id)
            .MaxAsync(s => (int?)s.SortOrder) ?? 0;

        _db.AiExamSections.Add(new AiExamSection
        {
            AiExamConfigId    = Config!.Id,
            SectionName       = SectionName,
            SubjectId         = SectionSubjectId,
            NumberOfQuestions = SectionNumQuestions,
            SortOrder         = maxOrder + 1
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Section added.";
        return RedirectToPage(new { Id = Config.Id });
    }

    // ── Delete section ─────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteSectionAsync(int sectionId)
    {
        var section = await _db.AiExamSections.FindAsync(sectionId);
        if (section is not null)
        {
            _db.AiExamSections.Remove(section);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { Id = section?.AiExamConfigId ?? Id });
    }

    // ── Add topic ──────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddTopicAsync()
    {
        if (string.IsNullOrWhiteSpace(TopicName))
        {
            TempData["Error"] = "Topic name is required.";
            return RedirectToPage(new { Id });
        }

        var maxOrder = await _db.AiExamTopics
            .Where(t => t.AiExamSectionId == TopicSectionId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;

        _db.AiExamTopics.Add(new AiExamTopic
        {
            AiExamSectionId = TopicSectionId,
            TopicName       = TopicName,
            TopicId         = TopicTopicId,
            SyllabusText    = TopicSyllabus,
            SortOrder       = maxOrder + 1
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Topic added.";
        return RedirectToPage(new { Id });
    }

    // ── Save prompt ─────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostSavePromptAsync()
    {
        await EnsureConfigAsync();

        // Deactivate old versions
        var old = await _db.GenerationPromptTemplates
            .Where(t => t.AiExamConfigId == Config!.Id && t.IsActive)
            .ToListAsync();
        old.ForEach(t => t.IsActive = false);

        // New version
        var maxVer = await _db.GenerationPromptTemplates
            .Where(t => t.AiExamConfigId == Config!.Id)
            .MaxAsync(t => (int?)t.Version) ?? 0;

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _db.GenerationPromptTemplates.Add(new GenerationPromptTemplate
        {
            AiExamConfigId = Config!.Id,
            Version        = maxVer + 1,
            IsActive       = true,
            PromptText     = PromptText,
            CreatedAt      = DateTime.UtcNow,
            CreatedBy      = userId is not null ? Guid.Parse(userId) : null
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Prompt template saved (v" + (maxVer + 1) + ").";
        return RedirectToPage(new { Id = Config.Id });
    }

    // ── Delete prompt (reset to built-in default) ───────────────────────────
    // Removes ALL saved prompt-template versions for this config, so generation
    // falls back to the built-in default prompt (subject/topic/exam driven).
    public async Task<IActionResult> OnPostDeletePromptAsync()
    {
        await EnsureConfigAsync();

        var templates = await _db.GenerationPromptTemplates
            .Where(t => t.AiExamConfigId == Config!.Id)
            .ToListAsync();

        if (templates.Count == 0)
        {
            TempData["Error"] = "No custom prompt template to delete.";
            return RedirectToPage(new { Id = Config!.Id });
        }

        _db.GenerationPromptTemplates.RemoveRange(templates);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Deleted {templates.Count} prompt version(s). " +
                              "Generation will now use the built-in default prompt.";
        return RedirectToPage(new { Id = Config!.Id });
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        SubjectOptions = await _db.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new ValueTuple<int, string>(s.Id, s.Name))
            .ToListAsync();

        TopicOptions = await _db.Topics
            .OrderBy(t => t.Name)
            .Select(t => new ValueTuple<int, string>(t.Id, t.Name))
            .ToListAsync();

        if (Id.HasValue)
        {
            Config = await _db.AiExamConfigs.FindAsync(Id.Value);
            if (Config is null) return;
        }
        else if (ExamPageId.HasValue)
        {
            Config = await _db.AiExamConfigs.FirstOrDefaultAsync(c => c.ExamPageId == ExamPageId);
            if (Config is null)
            {
                Config = new AiExamConfig { ExamPageId = ExamPageId.Value, IsAiEnabled = true };
                _db.AiExamConfigs.Add(Config);
                await _db.SaveChangesAsync();
            }
        }

        if (Config is not null)
        {
            var exam = await _db.ExamPages.FindAsync(Config.ExamPageId);
            ExamTitle = exam?.Title ?? "Unknown Exam";

            Sections = await _db.AiExamSections
                .Where(s => s.AiExamConfigId == Config.Id)
                .OrderBy(s => s.SortOrder)
                .ToListAsync();

            Topics = await _db.AiExamTopics
                .Where(t => Sections.Select(s => s.Id).Contains(t.AiExamSectionId))
                .OrderBy(t => t.AiExamSectionId)
                .ThenBy(t => t.SortOrder)
                .ToListAsync();

            ActiveTemplate = await _db.GenerationPromptTemplates
                .Where(t => t.AiExamConfigId == Config.Id && t.IsActive)
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync();
        }
    }

    private async Task EnsureConfigAsync()
    {
        await LoadAsync();
        if (Config is null)
            throw new InvalidOperationException("Config not found.");
    }

    private static bool IsSchemaNotReady(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is Npgsql.PostgresException pg && pg.SqlState is "42P01" or "42703")
                return true;
        return false;
    }
}
