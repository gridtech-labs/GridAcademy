using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Marketplace;
using GridAcademy.DTOs.Marketplace;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.Marketplace;

public class MarketplaceAdminService : IMarketplaceAdminService
{
    private readonly AppDbContext _db;
    private readonly ILogger<MarketplaceAdminService> _logger;

    public MarketplaceAdminService(AppDbContext db, ILogger<MarketplaceAdminService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────
    public async Task<MarketplaceDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalProviders   = await _db.MpProviders.CountAsync(ct);
        var pendingProviders = await _db.MpProviders.CountAsync(p => p.Status == ProviderStatus.Pending, ct);
        var totalSeries      = await _db.MpTestSeries.CountAsync(ct);
        var pendingReview    = await _db.MpTestSeries.CountAsync(s => s.Status == SeriesStatus.PendingReview, ct);
        var publishedSeries  = await _db.MpTestSeries.CountAsync(s => s.Status == SeriesStatus.Published, ct);

        var paidOrders = await _db.MpOrders
            .Where(o => o.Status == OrderStatus.Paid)
            .AsNoTracking()
            .ToListAsync(ct);

        var totalOrders         = paidOrders.Count;
        var gmvAll              = paidOrders.Sum(o => o.GrandTotal);
        var gmvMonth            = paidOrders.Where(o => o.CreatedAt >= monthStart).Sum(o => o.GrandTotal);

        var commissions = await _db.MpCommissions.AsNoTracking().ToListAsync(ct);
        var platformAll   = commissions.Sum(c => c.PlatformAmount);
        var platformMonth = commissions
            .Where(c => c.CreatedAt >= monthStart)
            .Sum(c => c.PlatformAmount);
        var pendingPayouts = commissions
            .Where(c => c.Status == CommissionStatus.Pending)
            .Sum(c => c.ProviderAmount);

        // Last 30 days GMV
        var last30 = Enumerable.Range(0, 30)
            .Select(i => DateOnly.FromDateTime(now.AddDays(-i)))
            .Select(d => new DailyGmvDto(d,
                paidOrders.Where(o => DateOnly.FromDateTime(o.CreatedAt) == d).Sum(o => o.GrandTotal),
                paidOrders.Count(o => DateOnly.FromDateTime(o.CreatedAt) == d)))
            .OrderBy(d => d.Date)
            .ToList();

        var topSeries = await _db.MpTestSeries
            .Where(s => s.Status == SeriesStatus.Published)
            .Include(s => s.Provider)
            .OrderByDescending(s => s.PurchaseCount)
            .Take(5)
            .AsNoTracking()
            .Select(s => new TopSeriesDto(s.Id, s.Title, s.Provider.InstituteName, s.PurchaseCount, 0))
            .ToListAsync(ct);

        return new MarketplaceDashboardDto(
            totalProviders, pendingProviders, totalSeries, pendingReview, publishedSeries,
            totalOrders, gmvAll, gmvMonth, platformAll, platformMonth, pendingPayouts,
            last30, topSeries);
    }

    // ── Providers ─────────────────────────────────────────────────────────────
    public async Task<PagedResult<AdminProviderListDto>> GetProvidersAsync(int page, int pageSize, string? status, CancellationToken ct = default)
    {
        var query = _db.MpProviders
            .Include(p => p.User)
            .Include(p => p.TestSeries)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProviderStatus>(status, true, out var s))
            query = query.Where(p => p.Status == s);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminProviderListDto(
                p.Id,
                p.UserId,
                p.User.FirstName + " " + p.User.LastName,
                p.User.Email,
                p.InstituteName,
                p.Status.ToString(),
                p.TestSeries.Count(s => s.Status == SeriesStatus.Published),
                p.TestSeries.Sum(s => s.PurchaseCount),
                p.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AdminProviderListDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ProviderDto> UpdateProviderStatusAsync(int providerId, string status, string? notes, CancellationToken ct = default)
    {
        var provider = await _db.MpProviders.FindAsync(new object[] { providerId }, ct)
            ?? throw new KeyNotFoundException("Provider not found.");

        if (!Enum.TryParse<ProviderStatus>(status, true, out var newStatus))
            throw new ArgumentException($"Invalid status '{status}'.");

        provider.Status     = newStatus;
        provider.AdminNotes = notes;
        provider.UpdatedAt  = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new ProviderDto(provider.Id, provider.UserId, provider.InstituteName,
            provider.City, provider.State, provider.Bio, provider.LogoUrl,
            provider.Status.ToString(), provider.AgreedToTerms, provider.CreatedAt);
    }

    // ── Review Queue ──────────────────────────────────────────────────────────
    public async Task<PagedResult<TestReviewQueueDto>> GetReviewQueueAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.MpTestSeries
            .Where(s => s.Status == SeriesStatus.PendingReview)
            .Include(s => s.Provider)
            .Include(s => s.ExamType)
            .AsNoTracking();

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new TestReviewQueueDto(
                s.Id, s.Title, s.Provider.InstituteName,
                s.ExamType.Name, s.SeriesType.ToString(), s.PriceInr, s.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<TestReviewQueueDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task ReviewSeriesAsync(Guid seriesId, ReviewDecisionRequest req, CancellationToken ct = default)
    {
        var series = await _db.MpTestSeries.FindAsync(new object[] { seriesId }, ct)
            ?? throw new KeyNotFoundException("Test series not found.");

        if (series.Status != SeriesStatus.PendingReview)
            throw new InvalidOperationException("Series is not pending review.");

        series.Status      = req.Approved ? SeriesStatus.Published : SeriesStatus.Rejected;
        series.ReviewNotes = req.Notes;
        series.UpdatedAt   = DateTime.UtcNow;

        if (req.Approved) series.PublishedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Series {SeriesId} {Decision} by admin.", seriesId, req.Approved ? "approved" : "rejected");
    }

    // ── Commissions ───────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<CommissionDto>> GetCommissionsAsync(int? providerId, bool pendingOnly, CancellationToken ct = default)
    {
        var query = _db.MpCommissions
            .Include(c => c.Order).ThenInclude(o => o.Series)
            .AsNoTracking()
            .AsQueryable();

        if (providerId.HasValue)
            query = query.Where(c => c.ProviderId == providerId.Value);

        if (pendingOnly)
            query = query.Where(c => c.Status == CommissionStatus.Pending);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommissionDto(
                c.Id, c.OrderId, c.Order.BookingRef, c.Order.Series.Title,
                c.GrossAmount, c.PlatformPct, c.PlatformAmount,
                c.ProviderPct, c.ProviderAmount, c.Status, c.PayoutId, c.CreatedAt))
            .ToListAsync(ct);
    }

    // ── Payouts ───────────────────────────────────────────────────────────────
    public async Task<PayoutDto> InitiatePayoutAsync(InitiatePayoutRequest req, CancellationToken ct = default)
    {
        var provider = await _db.MpProviders
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == req.ProviderId, ct)
            ?? throw new KeyNotFoundException("Provider not found.");

        var pendingCommissions = await _db.MpCommissions
            .Where(c => c.ProviderId == req.ProviderId && c.Status == CommissionStatus.Pending)
            .ToListAsync(ct);

        if (pendingCommissions.Count == 0)
            throw new InvalidOperationException("No pending commissions for this provider.");

        var totalAmount = pendingCommissions.Sum(c => c.ProviderAmount);

        var payout = new MpPayout
        {
            ProviderId  = req.ProviderId,
            Amount      = totalAmount,
            Status      = PayoutStatus.Initiated,
            InitiatedAt = DateTime.UtcNow
        };

        _db.MpPayouts.Add(payout);
        await _db.SaveChangesAsync(ct);

        // Link commissions to this payout
        foreach (var comm in pendingCommissions)
        {
            comm.Status   = CommissionStatus.Processed;
            comm.PayoutId = payout.Id;
        }
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Payout {PayoutId} initiated for provider {ProviderId}: ₹{Amount}",
            payout.Id, req.ProviderId, totalAmount);

        return new PayoutDto(
            payout.Id,
            req.ProviderId,
            provider.InstituteName,
            totalAmount,
            pendingCommissions.Count,
            payout.Status,
            payout.RazorpayTransferId,
            req.Notes,
            payout.InitiatedAt,
            payout.CompletedAt);
    }

    public async Task<IReadOnlyList<PayoutDto>> GetPayoutsAsync(int? providerId, CancellationToken ct = default)
    {
        var query = _db.MpPayouts
            .Include(p => p.Provider)
            .Include(p => p.Commissions)
            .AsNoTracking()
            .AsQueryable();

        if (providerId.HasValue)
            query = query.Where(p => p.ProviderId == providerId.Value);

        return await query
            .OrderByDescending(p => p.InitiatedAt)
            .Select(p => new PayoutDto(
                p.Id, p.ProviderId, p.Provider.InstituteName, p.Amount,
                p.Commissions.Count, p.Status, p.RazorpayTransferId,
                null, p.InitiatedAt, p.CompletedAt))
            .ToListAsync(ct);
    }

    // ── Banners ───────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<CmsBannerDto>> GetBannersAsync(CancellationToken ct = default)
    {
        return await _db.MpCmsBanners
            .OrderBy(b => b.SortOrder)
            .AsNoTracking()
            .Select(b => new CmsBannerDto(b.Id, b.Title, b.SubTitle, b.ImageUrl, b.LinkUrl, b.SortOrder))
            .ToListAsync(ct);
    }

    public async Task<CmsBannerDto> CreateBannerAsync(CreateBannerRequest req, CancellationToken ct = default)
    {
        var banner = new MpCmsBanner
        {
            Title     = req.Title,
            SubTitle  = req.SubTitle,
            ImageUrl  = req.ImageUrl,
            LinkUrl   = req.LinkUrl,
            SortOrder = req.SortOrder,
            ValidFrom = req.ValidFrom,
            ValidTo   = req.ValidTo,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.MpCmsBanners.Add(banner);
        await _db.SaveChangesAsync(ct);
        return new CmsBannerDto(banner.Id, banner.Title, banner.SubTitle, banner.ImageUrl, banner.LinkUrl, banner.SortOrder);
    }

    public async Task<CmsBannerDto> UpdateBannerAsync(int id, UpdateBannerRequest req, CancellationToken ct = default)
    {
        var banner = await _db.MpCmsBanners.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException("Banner not found.");

        banner.Title     = req.Title;
        banner.SubTitle  = req.SubTitle;
        banner.ImageUrl  = req.ImageUrl;
        banner.LinkUrl   = req.LinkUrl;
        banner.SortOrder = req.SortOrder;
        banner.IsActive  = req.IsActive;
        banner.ValidFrom = req.ValidFrom;
        banner.ValidTo   = req.ValidTo;
        await _db.SaveChangesAsync(ct);
        return new CmsBannerDto(banner.Id, banner.Title, banner.SubTitle, banner.ImageUrl, banner.LinkUrl, banner.SortOrder);
    }

    public async Task DeleteBannerAsync(int id, CancellationToken ct = default)
    {
        var banner = await _db.MpCmsBanners.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException("Banner not found.");
        _db.MpCmsBanners.Remove(banner);
        await _db.SaveChangesAsync(ct);
    }

    // ── Promo Codes ───────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<PromoCodeDto>> GetPromoCodesAsync(CancellationToken ct = default)
    {
        return await _db.MpPromoCodes
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .Select(p => new PromoCodeDto(
                p.Id, p.Code, p.DiscountType.ToString(), p.DiscountValue,
                p.MinOrderAmount, p.MaxDiscount, p.SeriesId, p.UsageLimit,
                p.UsedCount, p.IsActive, p.ValidFrom, p.ValidTo, p.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<PromoCodeDto> CreatePromoCodeAsync(CreatePromoCodeRequest req, CancellationToken ct = default)
    {
        var code = req.Code.Trim().ToUpperInvariant();
        if (await _db.MpPromoCodes.AnyAsync(p => p.Code == code, ct))
            throw new InvalidOperationException($"Promo code '{code}' already exists.");

        var promo = new MpPromoCode
        {
            Code          = code,
            DiscountType  = req.DiscountType,
            DiscountValue = req.DiscountValue,
            MinOrderAmount = req.MinOrderAmount,
            MaxDiscount   = req.MaxDiscount,
            SeriesId      = req.SeriesId,
            UsageLimit    = req.UsageLimit,
            ValidFrom     = req.ValidFrom,
            ValidTo       = req.ValidTo,
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow
        };
        _db.MpPromoCodes.Add(promo);
        await _db.SaveChangesAsync(ct);

        return new PromoCodeDto(promo.Id, promo.Code, promo.DiscountType.ToString(),
            promo.DiscountValue, promo.MinOrderAmount, promo.MaxDiscount,
            promo.SeriesId, promo.UsageLimit, promo.UsedCount, promo.IsActive,
            promo.ValidFrom, promo.ValidTo, promo.CreatedAt);
    }

    public async Task TogglePromoCodeAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var promo = await _db.MpPromoCodes.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException("Promo code not found.");
        promo.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }
}
