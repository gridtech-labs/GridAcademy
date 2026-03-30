using System.Security.Claims;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Marketplace;
using GridAcademy.Data.Entities.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.Marketplace.Series;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<SeriesRow>             AllSeries    { get; set; } = [];
    public List<(int Id, string Name)> ExamTypes    { get; set; } = [];
    public List<(Guid Id, string Title)> AllTests   { get; set; } = [];

    [TempData] public string? SuccessMsg { get; set; }
    [TempData] public string? ErrorMsg   { get; set; }

    // ── Create form bound properties ──────────────────────────────────────
    [BindProperty] public string?  NewTitle          { get; set; }
    [BindProperty] public string?  NewShortDesc      { get; set; }
    [BindProperty] public int      NewExamTypeId     { get; set; }
    [BindProperty] public int      NewSeriesType     { get; set; }
    [BindProperty] public decimal  NewPrice          { get; set; }
    [BindProperty] public bool     NewFirstTestFree  { get; set; }
    [BindProperty] public List<Guid> NewTestIds      { get; set; } = [];
    [BindProperty] public bool     PublishNow        { get; set; }

    public async Task OnGetAsync()
    {
        await LoadPageDataAsync();
    }

    // ── CREATE ────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTitle))
        {
            ErrorMsg = "Title is required.";
            await LoadPageDataAsync();
            return Page();
        }

        var userId   = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var provider = await EnsurePlatformProviderAsync(userId);

        // Generate slug from title
        var slug = GenerateSlug(NewTitle);
        var slugExists = await _db.MpTestSeries.AnyAsync(s => s.Slug == slug);
        if (slugExists) slug = $"{slug}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var series = new MpTestSeries
        {
            ProviderId       = provider.Id,
            ExamTypeId       = NewExamTypeId,
            Title            = NewTitle.Trim(),
            Slug             = slug,
            ShortDescription = NewShortDesc?.Trim(),
            PriceInr         = NewPrice,
            SeriesType       = (SeriesType)NewSeriesType,
            IsFirstTestFree  = NewFirstTestFree,
            Status           = PublishNow ? SeriesStatus.Published : SeriesStatus.Draft,
            PublishedAt      = PublishNow ? DateTime.UtcNow : null,
            IsActive         = true,
            Language         = "English"
        };

        _db.MpTestSeries.Add(series);
        await _db.SaveChangesAsync();

        // Link tests
        int order = 1;
        foreach (var testId in NewTestIds)
        {
            _db.MpSeriesTests.Add(new MpSeriesTest
            {
                SeriesId       = series.Id,
                TestId         = testId,
                SortOrder      = order++,
                IsFreePreview  = order == 2 && NewFirstTestFree   // first test is free preview
            });
        }
        if (NewTestIds.Any()) await _db.SaveChangesAsync();

        SuccessMsg = PublishNow
            ? $"Series \"{series.Title}\" created and published with {NewTestIds.Count} test(s). It is now live on the storefront."
            : $"Series \"{series.Title}\" created as Draft with {NewTestIds.Count} test(s).";

        return RedirectToPage();
    }

    // ── PUBLISH / UNPUBLISH ───────────────────────────────────────────────
    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        var series = await _db.MpTestSeries.FindAsync(id);
        if (series == null) { ErrorMsg = "Series not found."; return RedirectToPage(); }

        series.Status      = SeriesStatus.Published;
        series.PublishedAt = DateTime.UtcNow;
        series.UpdatedAt   = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        SuccessMsg = $"\"{series.Title}\" is now live on the storefront.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnpublishAsync(Guid id)
    {
        var series = await _db.MpTestSeries.FindAsync(id);
        if (series == null) { ErrorMsg = "Series not found."; return RedirectToPage(); }

        series.Status    = SeriesStatus.Draft;
        series.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        SuccessMsg = $"\"{series.Title}\" moved to Draft.";
        return RedirectToPage();
    }

    // ── DELETE ────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var series = await _db.MpTestSeries
            .Include(s => s.SeriesTests)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (series == null) { ErrorMsg = "Series not found."; return RedirectToPage(); }

        _db.MpSeriesTests.RemoveRange(series.SeriesTests);
        _db.MpTestSeries.Remove(series);
        await _db.SaveChangesAsync();

        SuccessMsg = $"\"{series.Title}\" deleted.";
        return RedirectToPage();
    }

    // ── ADD TEST TO EXISTING SERIES ───────────────────────────────────────
    public async Task<IActionResult> OnPostAddTestAsync(Guid seriesId, Guid testId)
    {
        var already = await _db.MpSeriesTests.AnyAsync(t => t.SeriesId == seriesId && t.TestId == testId);
        if (!already)
        {
            var maxOrder = await _db.MpSeriesTests.Where(t => t.SeriesId == seriesId).MaxAsync(t => (int?)t.SortOrder) ?? 0;
            _db.MpSeriesTests.Add(new MpSeriesTest { SeriesId = seriesId, TestId = testId, SortOrder = maxOrder + 1 });
            await _db.SaveChangesAsync();
            SuccessMsg = "Test added to series.";
        }
        return RedirectToPage();
    }

    // ── REMOVE TEST FROM SERIES ───────────────────────────────────────────
    public async Task<IActionResult> OnPostRemoveTestAsync(int id)
    {
        var link = await _db.MpSeriesTests.FindAsync(id);
        if (link != null) { _db.MpSeriesTests.Remove(link); await _db.SaveChangesAsync(); SuccessMsg = "Test removed."; }
        return RedirectToPage();
    }

    // ── HELPERS ───────────────────────────────────────────────────────────

    private async Task LoadPageDataAsync()
    {
        AllSeries = await _db.MpTestSeries
            .Include(s => s.ExamType)
            .Include(s => s.SeriesTests).ThenInclude(t => t.Test)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SeriesRow
            {
                Id             = s.Id,
                Title          = s.Title,
                Slug           = s.Slug,
                ExamTypeName   = s.ExamType.Name,
                SeriesType     = s.SeriesType.ToString(),
                PriceInr       = s.PriceInr,
                Status         = s.Status,
                TestCount      = s.SeriesTests.Count,
                Tests          = s.SeriesTests
                                  .OrderBy(t => t.SortOrder)
                                  .Select(t => new SeriesTestRow { LinkId = t.Id, TestId = t.TestId, Title = t.Test.Title, IsFreePreview = t.IsFreePreview })
                                  .ToList(),
                CreatedAt      = s.CreatedAt,
                PublishedAt    = s.PublishedAt
            })
            .ToListAsync();

        ExamTypes = await _db.ExamTypes
            .Where(e => e.IsActive)
            .OrderBy(e => e.SortOrder)
            .Select(e => new { e.Id, e.Name })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(e => (e.Id, e.Name)).ToList());

        AllTests = await _db.Tests
            .Where(t => t.Status == TestStatus.Published)
            .OrderBy(t => t.Title)
            .Select(t => new { t.Id, t.Title })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(x => (x.Id, x.Title)).ToList());
    }

    private async Task<MpProvider> EnsurePlatformProviderAsync(Guid userId)
    {
        // Use existing provider for this admin, or create a platform provider
        var provider = await _db.MpProviders.FirstOrDefaultAsync(p => p.UserId == userId);
        if (provider != null) return provider;

        provider = new MpProvider
        {
            UserId           = userId,
            InstituteName    = "GridAcademy Official",
            Status           = ProviderStatus.Verified,
            AgreedToTerms    = true,
            AgreedAt         = DateTime.UtcNow
        };
        _db.MpProviders.Add(provider);
        await _db.SaveChangesAsync();
        return provider;
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        slug = slug.Trim('-');
        return slug.Length > 200 ? slug[..200] : slug;
    }

    // ── View models ───────────────────────────────────────────────────────
    public class SeriesRow
    {
        public Guid                Id           { get; set; }
        public string              Title        { get; set; } = "";
        public string              Slug         { get; set; } = "";
        public string              ExamTypeName { get; set; } = "";
        public string              SeriesType   { get; set; } = "";
        public decimal             PriceInr     { get; set; }
        public SeriesStatus        Status       { get; set; }
        public int                 TestCount    { get; set; }
        public List<SeriesTestRow> Tests        { get; set; } = [];
        public DateTime            CreatedAt    { get; set; }
        public DateTime?           PublishedAt  { get; set; }
    }

    public class SeriesTestRow
    {
        public int    LinkId        { get; set; }
        public Guid   TestId        { get; set; }
        public string Title         { get; set; } = "";
        public bool   IsFreePreview { get; set; }
    }
}
