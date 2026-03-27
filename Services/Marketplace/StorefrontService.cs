using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Marketplace;
using GridAcademy.DTOs.Marketplace;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.Marketplace;

public class StorefrontService : IStorefrontService
{
    private readonly AppDbContext _db;

    public StorefrontService(AppDbContext db) => _db = db;

    // ── Homepage ──────────────────────────────────────────────────────────────
    public async Task<HomepageDto> GetHomepageAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var banners = await _db.MpCmsBanners
            .Where(b => b.IsActive
                     && (b.ValidFrom == null || b.ValidFrom <= now)
                     && (b.ValidTo   == null || b.ValidTo   >= now))
            .OrderBy(b => b.SortOrder)
            .Select(b => new CmsBannerDto(b.Id, b.Title, b.SubTitle, b.ImageUrl, b.LinkUrl, b.SortOrder))
            .ToListAsync(ct);

        var examCategories = await _db.ExamTypes
            .Select(e => new ExamCategoryDto(
                e.Id,
                e.Name,
                null,   // Emoji can be added as a field later
                _db.MpTestSeries.Count(s => s.ExamTypeId == e.Id && s.Status == SeriesStatus.Published && s.IsActive)))
            .OrderBy(e => e.Id)
            .ToListAsync(ct);

        var publishedBase = _db.MpTestSeries
            .Where(s => s.Status == SeriesStatus.Published && s.IsActive)
            .Include(s => s.Provider).ThenInclude(p => p.User)
            .Include(s => s.ExamType)
            .Include(s => s.SeriesTests)
            .AsNoTracking();

        var freeTests  = await publishedBase
            .Where(s => s.PriceInr == 0 || s.IsFirstTestFree)
            .OrderByDescending(s => s.PurchaseCount)
            .Take(8).Select(s => MapToListDto(s)).ToListAsync(ct);

        var topSelling = await publishedBase
            .OrderByDescending(s => s.PurchaseCount)
            .Take(8).Select(s => MapToListDto(s)).ToListAsync(ct);

        var newArrivals = await publishedBase
            .OrderByDescending(s => s.PublishedAt)
            .Take(8).Select(s => MapToListDto(s)).ToListAsync(ct);

        return new HomepageDto(banners, examCategories, freeTests, topSelling, newArrivals);
    }

    // ── Search / Browse ───────────────────────────────────────────────────────
    public async Task<PagedResult<TestSeriesListDto>> SearchSeriesAsync(TestSeriesSearchRequest req, CancellationToken ct = default)
    {
        var query = _db.MpTestSeries
            .Where(s => s.Status == SeriesStatus.Published && s.IsActive)
            .Include(s => s.Provider).ThenInclude(p => p.User)
            .Include(s => s.ExamType)
            .Include(s => s.SeriesTests)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Query))
        {
            var term = req.Query.ToLower();
            query = query.Where(s =>
                s.Title.ToLower().Contains(term) ||
                s.ShortDescription!.ToLower().Contains(term));
        }

        if (req.ExamTypeId.HasValue)
            query = query.Where(s => s.ExamTypeId == req.ExamTypeId.Value);

        if (req.SeriesType.HasValue)
            query = query.Where(s => s.SeriesType == req.SeriesType.Value);

        if (req.MinPrice.HasValue)
            query = query.Where(s => s.PriceInr >= req.MinPrice.Value);

        if (req.MaxPrice.HasValue)
            query = query.Where(s => s.PriceInr <= req.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(req.Language))
            query = query.Where(s => s.Language == req.Language);

        query = req.SortBy switch
        {
            "newest"     => query.OrderByDescending(s => s.PublishedAt),
            "price_asc"  => query.OrderBy(s => s.PriceInr),
            "price_desc" => query.OrderByDescending(s => s.PriceInr),
            "rating"     => query.OrderByDescending(s => s.AvgRating),
            _            => query.OrderByDescending(s => s.PurchaseCount)  // "popular"
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(s => MapToListDto(s))
            .ToListAsync(ct);

        return new PagedResult<TestSeriesListDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = req.Page,
            PageSize   = req.PageSize
        };
    }

    // ── Series Detail ─────────────────────────────────────────────────────────
    public async Task<TestSeriesDetailDto> GetSeriesDetailAsync(string slug, CancellationToken ct = default)
    {
        var series = await _db.MpTestSeries
            .Where(s => s.Slug == slug && s.Status == SeriesStatus.Published && s.IsActive)
            .Include(s => s.Provider).ThenInclude(p => p.User)
            .Include(s => s.ExamType)
            .Include(s => s.SeriesTests)
            .Include(s => s.Reviews.Where(r => r.IsVisible).OrderByDescending(r => r.CreatedAt).Take(5))
                .ThenInclude(r => r.Student)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Test series '{slug}' not found.");

        var tests = series.SeriesTests
            .OrderBy(t => t.SortOrder)
            .Select(st => new SeriesTestDto(
                st.TestId,
                $"Test {st.SortOrder + 1}",   // Title resolved from assessment module if needed
                st.SortOrder,
                st.IsFreePreview,
                0,   // TotalQuestions — can be enriched from ITestService if needed
                0))  // DurationMinutes
            .ToList();

        var reviews = series.Reviews
            .Where(r => r.IsVisible)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new ReviewDto(
                r.Id,
                r.StudentId,
                r.Student.FullName,
                r.Rating,
                r.Comment,
                r.CreatedAt))
            .ToList();

        return new TestSeriesDetailDto(
            series.Id,
            series.Title,
            series.Slug,
            series.ExamTypeId,
            series.ExamType.Name,
            series.ProviderId,
            series.Provider.InstituteName,
            series.Provider.LogoUrl,
            series.Provider.Bio,
            series.SeriesType.ToString(),
            series.ShortDescription,
            series.FullDescription,
            series.ThumbnailUrl,
            series.PriceInr,
            series.IsFirstTestFree,
            series.Language,
            series.Status.ToString(),
            series.SeriesTests.Count,
            series.PurchaseCount,
            series.AvgRating,
            series.ReviewCount,
            series.PublishedAt,
            tests,
            reviews);
    }

    // ── Promo Code Validation ─────────────────────────────────────────────────
    public async Task<PromoCodeValidateResponse> ValidatePromoCodeAsync(PromoCodeValidateRequest req, CancellationToken ct = default)
    {
        var now  = DateTime.UtcNow;
        var code = req.Code.Trim().ToUpperInvariant();

        var promo = await _db.MpPromoCodes
            .FirstOrDefaultAsync(p =>
                p.Code == code &&
                p.IsActive &&
                (p.ValidFrom == null || p.ValidFrom <= now) &&
                (p.ValidTo   == null || p.ValidTo   >= now), ct);

        if (promo is null)
            return new PromoCodeValidateResponse(false, "Invalid or expired promo code.", 0, "Flat", req.OrderAmount);

        if (promo.UsageLimit.HasValue && promo.UsedCount >= promo.UsageLimit.Value)
            return new PromoCodeValidateResponse(false, "Promo code usage limit has been reached.", 0, "Flat", req.OrderAmount);

        if (promo.MinOrderAmount.HasValue && req.OrderAmount < promo.MinOrderAmount.Value)
            return new PromoCodeValidateResponse(false,
                $"Minimum order amount ₹{promo.MinOrderAmount:N0} required.", 0, "Flat", req.OrderAmount);

        if (promo.SeriesId.HasValue && promo.SeriesId.Value != req.SeriesId)
            return new PromoCodeValidateResponse(false, "Promo code is not valid for this series.", 0, "Flat", req.OrderAmount);

        decimal discount = promo.DiscountType == DiscountType.Percentage
            ? req.OrderAmount * promo.DiscountValue / 100
            : promo.DiscountValue;

        if (promo.MaxDiscount.HasValue && discount > promo.MaxDiscount.Value)
            discount = promo.MaxDiscount.Value;

        discount = Math.Min(discount, req.OrderAmount);
        var final = req.OrderAmount - discount;

        return new PromoCodeValidateResponse(
            true,
            null,
            Math.Round(discount, 2),
            promo.DiscountType.ToString(),
            Math.Round(final, 2));
    }

    // ── Mapper ────────────────────────────────────────────────────────────────
    private static TestSeriesListDto MapToListDto(MpTestSeries s) => new(
        s.Id,
        s.Title,
        s.Slug,
        s.ExamType.Name,
        s.Provider.InstituteName,
        s.Provider.LogoUrl,
        s.SeriesType.ToString(),
        s.PriceInr,
        s.IsFirstTestFree,
        s.Language,
        s.Status.ToString(),
        s.SeriesTests.Count,
        s.PurchaseCount,
        s.AvgRating,
        s.ReviewCount,
        s.ThumbnailUrl,
        s.PublishedAt);
}
