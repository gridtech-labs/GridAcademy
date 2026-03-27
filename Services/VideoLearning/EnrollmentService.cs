using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.VideoLearning;

public class EnrollmentService(AppDbContext db) : IEnrollmentService
{
    private static EnrollmentDto Map(VlEnrollment e) => new(
        e.Id, e.UserId, e.User?.Email ?? "", e.ProgramId, e.Program?.Title ?? "",
        e.PricingPlanId, e.PricingPlan?.Name ?? "",
        e.Status, e.AmountPaidInr, e.AmountPaidUsd,
        e.CouponCode, e.DiscountApplied,
        e.ChannelId, e.Channel?.Name, e.EnrolledAt, e.ExpiresAt);

    private IQueryable<VlEnrollment> BaseQuery() =>
        db.VlEnrollments.Include(e => e.User).Include(e => e.Program)
            .Include(e => e.PricingPlan).Include(e => e.Channel);

    public async Task<PagedResult<EnrollmentDto>> GetEnrollmentsAsync(EnrollmentListRequest request)
    {
        var q = BaseQuery();
        if (request.UserId.HasValue)   q = q.Where(e => e.UserId == request.UserId.Value);
        if (request.ProgramId.HasValue) q = q.Where(e => e.ProgramId == request.ProgramId.Value);
        if (request.Status.HasValue)   q = q.Where(e => e.Status == request.Status.Value);
        if (!string.IsNullOrWhiteSpace(request.UserEmail))
            q = q.Where(e => e.User!.Email.Contains(request.UserEmail));
        if (request.FromDate.HasValue) q = q.Where(e => e.EnrolledAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)   q = q.Where(e => e.EnrolledAt <= request.ToDate.Value);

        var total = await q.CountAsync();
        var items = (await q.OrderByDescending(e => e.EnrolledAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync())
            .Select(Map).ToList();
        return new PagedResult<EnrollmentDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<EnrollmentDto> GetByIdAsync(Guid id)
    {
        var e = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Enrollment {id} not found.");
        return Map(e);
    }

    public async Task<EnrollmentDto> EnrollAsync(CreateEnrollmentRequest request)
    {
        if (await db.VlEnrollments.AnyAsync(e => e.UserId == request.UserId && e.ProgramId == request.ProgramId && e.Status == EnrollmentStatus.Active))
            throw new InvalidOperationException("User is already actively enrolled in this program.");

        var expiresAt = request.ExpiresAt;
        if (!expiresAt.HasValue)
        {
            var plan = await db.VlProgramPricingPlans.FindAsync(request.PricingPlanId);
            if (plan?.ValidityDays.HasValue == true)
                expiresAt = DateTime.UtcNow.AddDays(plan.ValidityDays.Value);
        }

        var entity = new VlEnrollment {
            UserId = request.UserId, ProgramId = request.ProgramId, PricingPlanId = request.PricingPlanId,
            AmountPaidInr = request.AmountPaidInr, AmountPaidUsd = request.AmountPaidUsd,
            CouponCode = request.CouponCode, DiscountApplied = request.DiscountApplied,
            ChannelId = request.ChannelId, EnrolledAt = DateTime.UtcNow,
            ExpiresAt = expiresAt, UpdatedAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Active };
        db.VlEnrollments.Add(entity);
        await db.SaveChangesAsync();
        return await GetByIdAsync(entity.Id);
    }

    public async Task CancelAsync(Guid id)
    {
        var entity = await db.VlEnrollments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Enrollment {id} not found.");
        entity.Status = EnrollmentStatus.Cancelled; entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public Task<bool> IsEnrolledAsync(Guid userId, Guid programId) =>
        db.VlEnrollments.AnyAsync(e => e.UserId == userId && e.ProgramId == programId && e.Status == EnrollmentStatus.Active);

    public async Task<List<VideoProgressDto>> GetProgressAsync(Guid enrollmentId)
    {
        return await db.VlVideoProgresses.Include(vp => vp.Video)
            .Where(vp => vp.EnrollmentId == enrollmentId)
            .Select(vp => new VideoProgressDto(vp.Id, vp.EnrollmentId, vp.VideoId,
                vp.Video.Title, vp.Status, vp.WatchedSeconds, vp.CompletedAt))
            .ToListAsync();
    }

    public async Task<VideoProgressDto> UpsertProgressAsync(Guid enrollmentId, Guid videoId, int watchedSeconds, bool markCompleted)
    {
        var existing = await db.VlVideoProgresses
            .FirstOrDefaultAsync(vp => vp.EnrollmentId == enrollmentId && vp.VideoId == videoId);

        if (existing == null)
        {
            existing = new VlVideoProgress { EnrollmentId = enrollmentId, VideoId = videoId };
            db.VlVideoProgresses.Add(existing);
        }

        existing.WatchedSeconds = Math.Max(existing.WatchedSeconds, watchedSeconds);
        existing.LastUpdatedAt  = DateTime.UtcNow;

        if (markCompleted && existing.Status != VideoProgressStatus.Completed)
        {
            existing.Status      = VideoProgressStatus.Completed;
            existing.CompletedAt = DateTime.UtcNow;
        }
        else if (!markCompleted && existing.Status == VideoProgressStatus.NotStarted && watchedSeconds > 0)
        {
            existing.Status = VideoProgressStatus.InProgress;
        }

        await db.SaveChangesAsync();

        var video = await db.VlVideos.FindAsync(videoId);
        return new VideoProgressDto(existing.Id, existing.EnrollmentId, existing.VideoId,
            video?.Title ?? "", existing.Status, existing.WatchedSeconds, existing.CompletedAt);
    }

    public async Task<EnrollmentProgressSummaryDto> GetProgressSummaryAsync(Guid enrollmentId)
    {
        var enrollment = await db.VlEnrollments
            .Include(e => e.Program)
                .ThenInclude(p => p.ProgramLearningPaths)
                    .ThenInclude(plp => plp.LearningPath)
                        .ThenInclude(lp => lp.Nodes)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId)
            ?? throw new KeyNotFoundException($"Enrollment {enrollmentId} not found.");

        // Collect all video ContentIds from VL-type nodes across all learning paths
        var allVideoIds = enrollment.Program.ProgramLearningPaths
            .SelectMany(plp => plp.LearningPath.Nodes
                .Where(n => n.NodeType == LpNodeType.Video && n.ContentId.HasValue)
                .Select(n => n.ContentId!.Value))
            .Distinct().ToList();

        var progresses = await db.VlVideoProgresses
            .Where(vp => vp.EnrollmentId == enrollmentId && allVideoIds.Contains(vp.VideoId))
            .ToListAsync();

        var total      = allVideoIds.Count;
        var completed  = progresses.Count(vp => vp.Status == VideoProgressStatus.Completed);
        var inProgress = progresses.Count(vp => vp.Status == VideoProgressStatus.InProgress);
        var pct        = total > 0 ? Math.Round((decimal)completed / total * 100, 1) : 0;

        return new EnrollmentProgressSummaryDto(total, completed, inProgress, total - completed - inProgress, pct);
    }
}
