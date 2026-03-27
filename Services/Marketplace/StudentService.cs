using GridAcademy.Data;
using GridAcademy.Data.Entities.Marketplace;
using GridAcademy.DTOs.Marketplace;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.Marketplace;

public class StudentService : IStudentService
{
    private readonly AppDbContext _db;
    private readonly ILogger<StudentService> _logger;

    public StudentService(AppDbContext db, ILogger<StudentService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────
    public async Task<StudentDashboardDto> GetDashboardAsync(Guid studentId, CancellationToken ct = default)
    {
        var purchased  = await GetPurchasedSeriesAsync(studentId, ct);
        var orders     = await GetOrdersAsync(studentId, ct);
        var totalPaid  = orders.Count(o => o.Status == "Paid");

        return new StudentDashboardDto(
            TotalPurchases  : totalPaid,
            TestsAttempted  : 0,     // TODO: wire to AssessmentService when mp-assignment bridge is built
            AvgScore        : 0,
            PurchasedSeries : purchased,
            RecentOrders    : orders.Take(5).ToList());
    }

    // ── Purchased Series ──────────────────────────────────────────────────────
    public async Task<IReadOnlyList<PurchasedSeriesDto>> GetPurchasedSeriesAsync(Guid studentId, CancellationToken ct = default)
    {
        return await _db.MpEntitlements
            .Where(e => e.StudentId == studentId && e.IsActive)
            .Include(e => e.Series)
                .ThenInclude(s => s.Provider)
            .Include(e => e.Series)
                .ThenInclude(s => s.ExamType)
            .Include(e => e.Series)
                .ThenInclude(s => s.SeriesTests)
            .AsNoTracking()
            .Select(e => new PurchasedSeriesDto(
                e.SeriesId,
                e.Series.Title,
                e.Series.ThumbnailUrl,
                e.Series.ExamType.Name,
                e.Series.Provider.InstituteName,
                e.Series.SeriesTests.Count,
                0,      // AttemptedTests — TODO: from assessment module
                e.GrantedAt,
                e.ExpiresAt))
            .ToListAsync(ct);
    }

    // ── Order History ─────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(Guid studentId, CancellationToken ct = default)
    {
        return await _db.MpOrders
            .Where(o => o.StudentId == studentId)
            .Include(o => o.Series)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .Select(o => new OrderDto(
                o.Id,
                o.BookingRef,
                o.SeriesId,
                o.Series.Title,
                o.Series.ThumbnailUrl,
                o.AmountInr,
                o.GstAmount,
                o.BookingFee,
                o.DiscountApplied,
                o.GrandTotal,
                o.PromoCodeApplied,
                o.Status.ToString(),
                o.CreatedAt))
            .ToListAsync(ct);
    }

    // ── Submit Review ─────────────────────────────────────────────────────────
    public async Task<ReviewDto> SubmitReviewAsync(Guid studentId, SubmitReviewRequest req, CancellationToken ct = default)
    {
        // Must have entitlement
        var hasAccess = await _db.MpEntitlements.AnyAsync(
            e => e.StudentId == studentId && e.SeriesId == req.SeriesId && e.IsActive, ct);
        if (!hasAccess)
            throw new UnauthorizedAccessException("You must purchase this series before reviewing it.");

        // One review per student per series
        var existing = await _db.MpReviews
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.SeriesId == req.SeriesId, ct);
        if (existing is not null)
            throw new InvalidOperationException("You have already reviewed this series.");

        var review = new MpReview
        {
            StudentId = studentId,
            SeriesId  = req.SeriesId,
            Rating    = req.Rating,
            Comment   = req.Comment,
            IsVisible = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.MpReviews.Add(review);
        await _db.SaveChangesAsync(ct);

        // Recalculate avg rating on the series
        await UpdateSeriesRatingAsync(req.SeriesId, ct);

        var student = await _db.Users.FindAsync(new object[] { studentId }, ct);

        return new ReviewDto(review.Id, studentId, student?.FullName ?? "Student", req.Rating, req.Comment, review.CreatedAt);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task UpdateSeriesRatingAsync(Guid seriesId, CancellationToken ct)
    {
        var stats = await _db.MpReviews
            .Where(r => r.SeriesId == seriesId && r.IsVisible)
            .GroupBy(_ => 1)
            .Select(g => new { Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(ct);

        var series = await _db.MpTestSeries.FindAsync(new object[] { seriesId }, ct);
        if (series is not null && stats is not null)
        {
            series.AvgRating   = (decimal)stats.Avg;
            series.ReviewCount = stats.Count;
            series.UpdatedAt   = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }
}
