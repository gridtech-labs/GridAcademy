using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Marketplace;
using GridAcademy.DTOs.Marketplace;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.Marketplace;

public class ProviderService : IProviderService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(AppDbContext db, ILogger<ProviderService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── Resolve provider from user id ─────────────────────────────────────────
    private async Task<MpProvider> GetProviderByUserAsync(Guid userId, CancellationToken ct)
    {
        return await _db.MpProviders.FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Provider profile not found for this user.");
    }

    // ── Profile ───────────────────────────────────────────────────────────────
    public async Task<ProviderDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var p = await GetProviderByUserAsync(userId, ct);
        return MapToDto(p);
    }

    public async Task<ProviderDto> UpdateProfileAsync(Guid userId, UpdateProviderProfileRequest req, CancellationToken ct = default)
    {
        var p = await GetProviderByUserAsync(userId, ct);
        p.InstituteName = req.InstituteName.Trim();
        p.City          = req.City;
        p.State         = req.State;
        p.Bio           = req.Bio;
        p.LogoUrl       = req.LogoUrl;
        p.UpdatedAt     = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return MapToDto(p);
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────
    public async Task<ProviderDashboardDto> GetDashboardAsync(Guid userId, CancellationToken ct = default)
    {
        var provider = await GetProviderByUserAsync(userId, ct);
        var pid = provider.Id;

        var series = await _db.MpTestSeries
            .Where(s => s.ProviderId == pid)
            .AsNoTracking()
            .ToListAsync(ct);

        var commissions = await _db.MpCommissions
            .Where(c => c.ProviderId == pid)
            .AsNoTracking()
            .ToListAsync(ct);

        var top = series
            .OrderByDescending(s => s.PurchaseCount)
            .Take(5)
            .Select(s => new ProviderSeriesSummaryDto(
                s.Id,
                s.Title,
                s.Status.ToString(),
                s.PurchaseCount,
                s.AvgRating,
                commissions
                    .Where(c => _db.MpOrders.AsNoTracking()
                        .Any(o => o.Id == c.OrderId && o.SeriesId == s.Id))
                    .Sum(c => c.ProviderAmount)))
            .ToList();

        return new ProviderDashboardDto(
            TotalSeries          : series.Count,
            PublishedSeries      : series.Count(s => s.Status == SeriesStatus.Published),
            DraftSeries          : series.Count(s => s.Status == SeriesStatus.Draft),
            PendingReviewSeries  : series.Count(s => s.Status == SeriesStatus.PendingReview),
            TotalSales           : series.Sum(s => s.PurchaseCount),
            TotalRevenue         : commissions.Sum(c => c.ProviderAmount),
            PendingPayout        : commissions.Where(c => c.Status == CommissionStatus.Pending).Sum(c => c.ProviderAmount),
            ProcessedPayout      : commissions.Where(c => c.Status == CommissionStatus.Processed).Sum(c => c.ProviderAmount),
            TopSeries            : top);
    }

    // ── Test Series CRUD ──────────────────────────────────────────────────────
    public async Task<TestSeriesListDto> CreateSeriesAsync(Guid userId, CreateTestSeriesRequest req, CancellationToken ct = default)
    {
        var provider = await GetProviderByUserAsync(userId, ct);
        if (provider.Status != ProviderStatus.Verified)
            throw new InvalidOperationException("Your provider profile must be verified before publishing test series.");

        var slug = Slugify(req.Title);

        var series = new MpTestSeries
        {
            ProviderId       = provider.Id,
            ExamTypeId       = req.ExamTypeId,
            Title            = req.Title.Trim(),
            Slug             = await EnsureUniqueSlugAsync(slug, null, ct),
            ShortDescription = req.ShortDescription,
            FullDescription  = req.FullDescription,
            ThumbnailUrl     = req.ThumbnailUrl,
            SeriesType       = req.SeriesType,
            PriceInr         = req.PriceInr,
            IsFirstTestFree  = req.IsFirstTestFree,
            Language         = req.Language,
            Status           = SeriesStatus.Draft,
            IsActive         = true,
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        };

        _db.MpTestSeries.Add(series);
        await _db.SaveChangesAsync(ct);

        // Reload with nav props for DTO
        return await _db.MpTestSeries
            .Where(s => s.Id == series.Id)
            .Include(s => s.Provider).ThenInclude(p => p.User)
            .Include(s => s.ExamType)
            .Include(s => s.SeriesTests)
            .AsNoTracking()
            .Select(s => MapToListDto(s))
            .FirstAsync(ct);
    }

    public async Task<TestSeriesListDto> UpdateSeriesAsync(Guid userId, Guid seriesId, UpdateTestSeriesRequest req, CancellationToken ct = default)
    {
        var provider = await GetProviderByUserAsync(userId, ct);
        var series   = await _db.MpTestSeries
            .FirstOrDefaultAsync(s => s.Id == seriesId && s.ProviderId == provider.Id, ct)
            ?? throw new KeyNotFoundException("Series not found.");

        if (series.Status == SeriesStatus.Published)
            throw new InvalidOperationException("Cannot edit a Published series. Unpublish it first.");

        series.Title            = req.Title.Trim();
        series.ExamTypeId       = req.ExamTypeId;
        series.SeriesType       = req.SeriesType;
        series.ShortDescription = req.ShortDescription;
        series.FullDescription  = req.FullDescription;
        series.ThumbnailUrl     = req.ThumbnailUrl;
        series.PriceInr         = req.PriceInr;
        series.IsFirstTestFree  = req.IsFirstTestFree;
        series.Language         = req.Language;
        series.UpdatedAt        = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await _db.MpTestSeries
            .Where(s => s.Id == series.Id)
            .Include(s => s.Provider).ThenInclude(p => p.User)
            .Include(s => s.ExamType)
            .Include(s => s.SeriesTests)
            .AsNoTracking()
            .Select(s => MapToListDto(s))
            .FirstAsync(ct);
    }

    public async Task DeleteSeriesAsync(Guid userId, Guid seriesId, CancellationToken ct = default)
    {
        var provider = await GetProviderByUserAsync(userId, ct);
        var series   = await _db.MpTestSeries
            .FirstOrDefaultAsync(s => s.Id == seriesId && s.ProviderId == provider.Id, ct)
            ?? throw new KeyNotFoundException("Series not found.");

        if (series.Status == SeriesStatus.Published)
            throw new InvalidOperationException("Cannot delete a Published series.");

        _db.MpTestSeries.Remove(series);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<TestSeriesListDto>> GetMySeriesAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var provider = await GetProviderByUserAsync(userId, ct);
        var query    = _db.MpTestSeries
            .Where(s => s.ProviderId == provider.Id)
            .Include(s => s.Provider).ThenInclude(p => p.User)
            .Include(s => s.ExamType)
            .Include(s => s.SeriesTests)
            .AsNoTracking();

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => MapToListDto(s))
            .ToListAsync(ct);

        return new PagedResult<TestSeriesListDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task SubmitForReviewAsync(Guid userId, Guid seriesId, CancellationToken ct = default)
    {
        var provider = await GetProviderByUserAsync(userId, ct);
        var series   = await _db.MpTestSeries
            .FirstOrDefaultAsync(s => s.Id == seriesId && s.ProviderId == provider.Id, ct)
            ?? throw new KeyNotFoundException("Series not found.");

        if (series.Status != SeriesStatus.Draft)
            throw new InvalidOperationException("Only Draft series can be submitted for review.");

        var testCount = await _db.MpSeriesTests.CountAsync(s => s.SeriesId == seriesId, ct);
        if (testCount == 0)
            throw new InvalidOperationException("Add at least one test before submitting for review.");

        series.Status    = SeriesStatus.PendingReview;
        series.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ── Series ↔ Test ──────────────────────────────────────────────────────────
    public async Task AddTestToSeriesAsync(Guid userId, Guid seriesId, Guid testId, int sortOrder, bool isFreePreview, CancellationToken ct = default)
    {
        var provider = await GetProviderByUserAsync(userId, ct);
        var series   = await _db.MpTestSeries
            .FirstOrDefaultAsync(s => s.Id == seriesId && s.ProviderId == provider.Id, ct)
            ?? throw new KeyNotFoundException("Series not found.");

        if (series.Status == SeriesStatus.Published)
            throw new InvalidOperationException("Cannot modify a Published series.");

        var alreadyLinked = await _db.MpSeriesTests.AnyAsync(
            st => st.SeriesId == seriesId && st.TestId == testId, ct);
        if (alreadyLinked)
            throw new InvalidOperationException("Test is already linked to this series.");

        _db.MpSeriesTests.Add(new MpSeriesTest
        {
            SeriesId       = seriesId,
            TestId         = testId,
            SortOrder      = sortOrder,
            IsFreePreview  = isFreePreview
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveTestFromSeriesAsync(Guid userId, Guid seriesId, Guid testId, CancellationToken ct = default)
    {
        var provider = await GetProviderByUserAsync(userId, ct);
        var series   = await _db.MpTestSeries
            .FirstOrDefaultAsync(s => s.Id == seriesId && s.ProviderId == provider.Id, ct)
            ?? throw new KeyNotFoundException("Series not found.");

        if (series.Status == SeriesStatus.Published)
            throw new InvalidOperationException("Cannot modify a Published series.");

        var link = await _db.MpSeriesTests
            .FirstOrDefaultAsync(st => st.SeriesId == seriesId && st.TestId == testId, ct)
            ?? throw new KeyNotFoundException("Test not linked to this series.");

        _db.MpSeriesTests.Remove(link);
        await _db.SaveChangesAsync(ct);
    }

    // ── Commissions ───────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<CommissionDto>> GetCommissionsAsync(Guid userId, CancellationToken ct = default)
    {
        var provider = await GetProviderByUserAsync(userId, ct);

        return await _db.MpCommissions
            .Where(c => c.ProviderId == provider.Id)
            .Include(c => c.Order).ThenInclude(o => o.Series)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .Select(c => new CommissionDto(
                c.Id,
                c.OrderId,
                c.Order.BookingRef,
                c.Order.Series.Title,
                c.GrossAmount,
                c.PlatformPct,
                c.PlatformAmount,
                c.ProviderPct,
                c.ProviderAmount,
                c.Status,
                c.PayoutId,
                c.CreatedAt))
            .ToListAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string Slugify(string text)
    {
        var slug = text.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-{2,}", "-");
        return slug.Trim('-');
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, Guid? excludeId, CancellationToken ct)
    {
        var slug = baseSlug;
        var i    = 1;
        while (await _db.MpTestSeries.AnyAsync(s =>
            s.Slug == slug && (excludeId == null || s.Id != excludeId), ct))
        {
            slug = $"{baseSlug}-{i++}";
        }
        return slug;
    }

    private static ProviderDto MapToDto(MpProvider p) => new(
        p.Id, p.UserId, p.InstituteName, p.City, p.State, p.Bio, p.LogoUrl,
        p.Status.ToString(), p.AgreedToTerms, p.CreatedAt);

    private static TestSeriesListDto MapToListDto(MpTestSeries s) => new(
        s.Id, s.Title, s.Slug, s.ExamType.Name, s.Provider.InstituteName,
        s.Provider.LogoUrl, s.SeriesType.ToString(), s.PriceInr,
        s.IsFirstTestFree, s.Language, s.Status.ToString(),
        s.SeriesTests.Count, s.PurchaseCount, s.AvgRating, s.ReviewCount,
        s.ThumbnailUrl, s.PublishedAt);
}
