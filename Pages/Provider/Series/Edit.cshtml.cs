using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Assessment;
using GridAcademy.Data.Entities.Marketplace;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Provider.Series;

[Authorize(Roles = "Provider")]
public class EditModel : PageModel
{
    private readonly IProviderService _providers;
    private readonly AppDbContext     _db;

    public EditModel(IProviderService providers, AppDbContext db)
    {
        _providers = providers;
        _db        = db;
    }

    [BindProperty]
    public SeriesForm Form { get; set; } = new();

    public TestSeriesListDto?                                       Series         { get; set; }
    public List<SeriesTestInfo>                                     LinkedTests    { get; set; } = [];
    public List<(Guid Id, string Title, int Duration, string Status)> AvailableTests { get; set; } = [];
    public List<(int Id, string Name)>                              ExamTypes      { get; set; } = [];
    public Guid                                                     SeriesId       { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct = default)
    {
        SeriesId = id;
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (!await LoadSeriesAsync(userId, id, ct)) return NotFound();
        await LoadExamTypesAsync(ct);
        await LoadAvailableTestsAsync(userId, ct);

        if (Series is not null)
        {
            Form = new SeriesForm
            {
                Title            = Series.Title,
                ExamTypeId       = 0, // will be loaded from entity below
                SeriesType       = Enum.TryParse<SeriesType>(Series.SeriesType, out var st) ? st : SeriesType.FullMock,
                ShortDescription = null,
                FullDescription  = null,
                ThumbnailUrl     = Series.ThumbnailUrl,
                PriceInr         = Series.PriceInr,
                IsFirstTestFree  = Series.IsFirstTestFree,
                Language         = Series.Language
            };

            // Load full details for ExamTypeId and descriptions
            var entity = await _db.MpTestSeries.FindAsync(new object[] { id }, ct);
            if (entity is not null)
            {
                Form.ExamTypeId       = entity.ExamTypeId;
                Form.ShortDescription = entity.ShortDescription;
                Form.FullDescription  = entity.FullDescription;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid id, CancellationToken ct = default)
    {
        SeriesId = id;
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await LoadExamTypesAsync(ct);
        await LoadAvailableTestsAsync(userId, ct);

        if (!ModelState.IsValid)
        {
            await LoadSeriesAsync(userId, id, ct);
            return Page();
        }

        try
        {
            await _providers.UpdateSeriesAsync(userId, id, new UpdateTestSeriesRequest(
                Form.Title,
                Form.ExamTypeId,
                Form.SeriesType,
                Form.ShortDescription,
                Form.FullDescription,
                Form.ThumbnailUrl,
                Form.PriceInr,
                Form.IsFirstTestFree,
                Form.Language
            ), ct);

            TempData["Success"] = "Series updated successfully.";
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            await LoadSeriesAsync(userId, id, ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAddTestAsync(Guid id, Guid testId, int sortOrder, bool isFreePreview, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            await _providers.AddTestToSeriesAsync(userId, id, testId, sortOrder, isFreePreview, ct);
            TempData["Success"] = "Test added to series.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveTestAsync(Guid id, Guid testId, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            await _providers.RemoveTestFromSeriesAsync(userId, id, testId, ct);
            TempData["Success"] = "Test removed from series.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid id, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            await _providers.SubmitForReviewAsync(userId, id, ct);
            TempData["Success"] = "Series submitted for review. The admin team will review it shortly.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<bool> LoadSeriesAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var provider = await _db.MpProviders.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (provider is null) return false;

        var entity = await _db.MpTestSeries
            .Include(s => s.SeriesTests)
                .ThenInclude(st => st.Test)
            .Include(s => s.ExamType)
            .FirstOrDefaultAsync(s => s.Id == id && s.ProviderId == provider.Id, ct);

        if (entity is null) return false;

        Series = new TestSeriesListDto(
            entity.Id,
            entity.Title,
            entity.Slug,
            entity.ExamType?.Name ?? "",
            provider.InstituteName,
            provider.LogoUrl,
            entity.SeriesType.ToString(),
            entity.PriceInr,
            entity.IsFirstTestFree,
            entity.Language,
            entity.Status.ToString(),
            entity.SeriesTests.Count,
            entity.PurchaseCount,
            entity.AvgRating,
            entity.ReviewCount,
            entity.ThumbnailUrl,
            entity.PublishedAt
        );

        SeriesId = id;

        LinkedTests = entity.SeriesTests
            .OrderBy(st => st.SortOrder)
            .Select(st => new SeriesTestInfo(
                st.TestId,
                st.Test?.Title ?? "(unknown)",
                st.SortOrder,
                st.IsFreePreview,
                st.Test?.DurationMinutes ?? 0
            ))
            .ToList();

        return true;
    }

    private async Task LoadExamTypesAsync(CancellationToken ct)
    {
        var raw = await _db.ExamTypes
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Select(e => new { e.Id, e.Name })
            .ToListAsync(ct);
        ExamTypes = raw.Select(x => (x.Id, x.Name)).ToList();
    }

    private async Task LoadAvailableTestsAsync(Guid userId, CancellationToken ct)
    {
        var raw = await _db.Tests
            .Where(t => t.CreatedBy == userId && t.Status == TestStatus.Published)
            .OrderBy(t => t.Title)
            .Select(t => new { t.Id, t.Title, t.DurationMinutes, Status = t.Status.ToString() })
            .ToListAsync(ct);
        AvailableTests = raw.Select(x => (x.Id, x.Title, x.DurationMinutes, x.Status)).ToList();
    }

    public record SeriesTestInfo(Guid TestId, string Title, int SortOrder, bool IsFreePreview, int DurationMinutes);

    public class SeriesForm
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Exam type is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select an exam type.")]
        [Display(Name = "Exam Type")]
        public int ExamTypeId { get; set; }

        [Display(Name = "Series Type")]
        public SeriesType SeriesType { get; set; } = SeriesType.FullMock;

        [MaxLength(500)]
        [Display(Name = "Short Description")]
        public string? ShortDescription { get; set; }

        [Display(Name = "Full Description")]
        public string? FullDescription { get; set; }

        [MaxLength(500)]
        [Display(Name = "Thumbnail URL")]
        public string? ThumbnailUrl { get; set; }

        [Range(0, 100000)]
        [Display(Name = "Price (INR)")]
        public decimal PriceInr { get; set; } = 0;

        [Display(Name = "Is First Test Free")]
        public bool IsFirstTestFree { get; set; } = false;

        [MaxLength(20)]
        public string Language { get; set; } = "English";
    }
}
