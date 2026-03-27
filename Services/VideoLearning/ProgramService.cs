using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridAcademy.Services.VideoLearning;

public class ProgramService(AppDbContext db, IWebHostEnvironment env,
    IOptions<VideoLearningStorageOptions> opts) : IProgramService
{
    private readonly VideoLearningStorageOptions _opts = opts.Value;

    private static PricingPlanDto MapPlan(VlProgramPricingPlan p) => new(
        p.Id, p.ProgramId, p.Name, p.PriceInr, p.PriceUsd,
        p.OriginalPriceInr, p.OriginalPriceUsd, p.ValidityDays, p.IsActive, p.SortOrder);

    private async Task<ProgramDto> LoadFullAsync(Guid id)
    {
        var p = await db.VlPrograms
            .Include(x => x.Domain)
            .Include(x => x.PricingPlans)
            .Include(x => x.ProgramLearningPaths)
                .ThenInclude(plp => plp.LearningPath)
                    .ThenInclude(lp => lp.Nodes)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Program {id} not found.");

        var lps = p.ProgramLearningPaths.OrderBy(x => x.SortOrder).Select(plp => {
            var lp = plp.LearningPath;
            var moduleCount = lp.Nodes.Count(n => n.NodeType == LpNodeType.Module);
            return new LearningPathDto(lp.Id, lp.DomainId, "", lp.Title, lp.Description,
                lp.ThumbnailPath, lp.IsActive, lp.SortOrder, lp.Nodes.Count, moduleCount, lp.CreatedAt);
        }).ToList();

        return new ProgramDto(p.Id, p.DomainId, p.Domain?.Name ?? "", p.Title,
            p.ShortDescription, p.Description, p.ThumbnailPath, p.Status,
            p.PricingPlans.OrderBy(x => x.SortOrder).Select(MapPlan).ToList(),
            lps, p.CreatedAt, p.UpdatedAt);
    }

    public async Task<PagedResult<ProgramSummaryDto>> GetProgramsAsync(ProgramListRequest request)
    {
        var q = db.VlPrograms.Include(p => p.Domain).AsQueryable();
        if (request.DomainId.HasValue) q = q.Where(p => p.DomainId == request.DomainId.Value);
        if (request.Status.HasValue)   q = q.Where(p => p.Status == request.Status.Value);
        if (!string.IsNullOrWhiteSpace(request.Search)) q = q.Where(p => p.Title.Contains(request.Search));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(p => new ProgramSummaryDto(p.Id, p.DomainId, p.Domain!.Name, p.Title,
                p.ShortDescription, p.ThumbnailPath, p.Status,
                p.ProgramLearningPaths.Count, p.PricingPlans.Count, p.CreatedAt))
            .ToListAsync();

        return new PagedResult<ProgramSummaryDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public Task<ProgramDto> GetByIdAsync(Guid id) => LoadFullAsync(id);

    public async Task<ProgramDto> CreateAsync(CreateProgramRequest request, IFormFile? thumbnail = null, Guid? createdBy = null)
    {
        var entity = new VlProgram
        {
            DomainId = request.DomainId, Title = request.Title,
            LearningCode = request.LearningCode, IsBlendedLearning = request.IsBlendedLearning,
            ShortDescription = request.ShortDescription, Description = request.Description,
            Status = request.Status, CreatedBy = createdBy, UpdatedBy = createdBy,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        if (thumbnail != null) entity.ThumbnailPath = await SaveThumbnailAsync(entity.Id, thumbnail);
        db.VlPrograms.Add(entity);

        if (request.Plans != null)
        {
            int order = 0;
            foreach (var plan in request.Plans)
                entity.PricingPlans.Add(new VlProgramPricingPlan {
                    ProgramId = entity.Id, Name = plan.Name,
                    PriceInr = plan.PriceInr, PriceUsd = plan.PriceUsd,
                    OriginalPriceInr = plan.OriginalPriceInr, OriginalPriceUsd = plan.OriginalPriceUsd,
                    ValidityDays = plan.ValidityDays, IsActive = plan.IsActive, SortOrder = order++ });
        }

        if (request.LearningPathIds != null)
        {
            int order = 0;
            foreach (var lpId in request.LearningPathIds)
                entity.ProgramLearningPaths.Add(new VlProgramLearningPath {
                    ProgramId = entity.Id, LearningPathId = lpId, SortOrder = order++ });
        }

        await db.SaveChangesAsync();
        return await LoadFullAsync(entity.Id);
    }

    public async Task<ProgramDto> UpdateAsync(Guid id, UpdateProgramRequest request, IFormFile? thumbnail = null, Guid? updatedBy = null)
    {
        var entity = await db.VlPrograms.FindAsync(id)
            ?? throw new KeyNotFoundException($"Program {id} not found.");
        entity.DomainId = request.DomainId; entity.Title = request.Title;
        entity.LearningCode = request.LearningCode; entity.IsBlendedLearning = request.IsBlendedLearning;
        entity.ShortDescription = request.ShortDescription; entity.Description = request.Description;
        entity.Status = request.Status; entity.UpdatedBy = updatedBy; entity.UpdatedAt = DateTime.UtcNow;
        if (thumbnail != null) entity.ThumbnailPath = await SaveThumbnailAsync(id, thumbnail);
        await db.SaveChangesAsync();
        return await LoadFullAsync(id);
    }

    public async Task PublishAsync(Guid id)
    {
        var entity = await db.VlPrograms.Include(p => p.PricingPlans).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Program {id} not found.");
        if (!entity.PricingPlans.Any(pp => pp.IsActive))
            throw new InvalidOperationException("Program must have at least one active pricing plan before publishing.");
        entity.Status = ProgramStatus.Published; entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task UnpublishAsync(Guid id)
    {
        var entity = await db.VlPrograms.FindAsync(id)
            ?? throw new KeyNotFoundException($"Program {id} not found.");
        entity.Status = ProgramStatus.Draft; entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await db.VlPrograms.FindAsync(id)
            ?? throw new KeyNotFoundException($"Program {id} not found.");
        if (await db.VlEnrollments.AnyAsync(e => e.ProgramId == id))
            throw new InvalidOperationException("Cannot delete program with existing enrollments.");
        db.VlPrograms.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<PricingPlanDto> AddPricingPlanAsync(Guid programId, CreatePricingPlanRequest request)
    {
        var plan = new VlProgramPricingPlan {
            ProgramId = programId, Name = request.Name,
            PriceInr = request.PriceInr, PriceUsd = request.PriceUsd,
            OriginalPriceInr = request.OriginalPriceInr, OriginalPriceUsd = request.OriginalPriceUsd,
            ValidityDays = request.ValidityDays, IsActive = request.IsActive, SortOrder = request.SortOrder };
        db.VlProgramPricingPlans.Add(plan);
        await db.SaveChangesAsync();
        return MapPlan(plan);
    }

    public async Task<PricingPlanDto> UpdatePricingPlanAsync(int planId, CreatePricingPlanRequest request)
    {
        var plan = await db.VlProgramPricingPlans.FindAsync(planId)
            ?? throw new KeyNotFoundException($"Pricing plan {planId} not found.");
        plan.Name = request.Name; plan.PriceInr = request.PriceInr; plan.PriceUsd = request.PriceUsd;
        plan.OriginalPriceInr = request.OriginalPriceInr; plan.OriginalPriceUsd = request.OriginalPriceUsd;
        plan.ValidityDays = request.ValidityDays; plan.IsActive = request.IsActive; plan.SortOrder = request.SortOrder;
        await db.SaveChangesAsync();
        return MapPlan(plan);
    }

    public async Task DeletePricingPlanAsync(int planId)
    {
        var plan = await db.VlProgramPricingPlans.FindAsync(planId)
            ?? throw new KeyNotFoundException($"Pricing plan {planId} not found.");
        if (await db.VlEnrollments.AnyAsync(e => e.PricingPlanId == planId))
            throw new InvalidOperationException("Cannot delete plan with existing enrollments.");
        db.VlProgramPricingPlans.Remove(plan);
        await db.SaveChangesAsync();
    }

    public async Task AddLearningPathAsync(Guid programId, Guid learningPathId, int sortOrder = 0)
    {
        if (await db.VlProgramLearningPaths.AnyAsync(x => x.ProgramId == programId && x.LearningPathId == learningPathId))
            return;
        db.VlProgramLearningPaths.Add(new VlProgramLearningPath { ProgramId = programId, LearningPathId = learningPathId, SortOrder = sortOrder });
        await db.SaveChangesAsync();
    }

    public async Task RemoveLearningPathAsync(Guid programId, Guid learningPathId)
    {
        var item = await db.VlProgramLearningPaths.FindAsync(programId, learningPathId);
        if (item == null) return;
        db.VlProgramLearningPaths.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task ReorderLearningPathsAsync(Guid programId, List<ReorderItem> items)
    {
        foreach (var item in items)
        {
            var plp = await db.VlProgramLearningPaths.FindAsync(programId, item.ItemId);
            if (plp != null) plp.SortOrder = item.SortOrder;
        }
        await db.SaveChangesAsync();
    }

    public async Task<CourseLaunchDto> AddCourseLaunchAsync(Guid programId, CreateCourseLaunchRequest request)
    {
        var launch = new VlCourseLaunch {
            ProgramId = programId, Name = request.Name, Status = request.Status,
            BlockedReason = request.BlockedReason, StartDate = request.StartDate,
            EndDate = request.EndDate, MaxEnrollments = request.MaxEnrollments,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VlCourseLaunches.Add(launch);
        await db.SaveChangesAsync();
        var prog = await db.VlPrograms.FindAsync(programId);
        return new CourseLaunchDto(launch.Id, programId, prog?.Title ?? "", launch.Name,
            launch.Status, launch.BlockedReason, launch.StartDate, launch.EndDate,
            launch.MaxEnrollments, launch.CreatedAt);
    }

    public async Task<CourseLaunchDto> UpdateCourseLaunchAsync(int launchId, CreateCourseLaunchRequest request)
    {
        var launch = await db.VlCourseLaunches.Include(l => l.Program)
            .FirstOrDefaultAsync(l => l.Id == launchId)
            ?? throw new KeyNotFoundException($"Launch {launchId} not found.");
        launch.Name = request.Name; launch.Status = request.Status;
        launch.BlockedReason = request.BlockedReason; launch.StartDate = request.StartDate;
        launch.EndDate = request.EndDate; launch.MaxEnrollments = request.MaxEnrollments;
        launch.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return new CourseLaunchDto(launch.Id, launch.ProgramId, launch.Program?.Title ?? "",
            launch.Name, launch.Status, launch.BlockedReason, launch.StartDate, launch.EndDate,
            launch.MaxEnrollments, launch.CreatedAt);
    }

    public async Task DeleteCourseLaunchAsync(int launchId)
    {
        var launch = await db.VlCourseLaunches.FindAsync(launchId)
            ?? throw new KeyNotFoundException($"Launch {launchId} not found.");
        db.VlCourseLaunches.Remove(launch);
        await db.SaveChangesAsync();
    }

    public async Task<List<CourseLaunchDto>> GetCourseLaunchesAsync(Guid programId)
    {
        return await db.VlCourseLaunches
            .Where(l => l.ProgramId == programId)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new CourseLaunchDto(l.Id, l.ProgramId, l.Program!.Title,
                l.Name, l.Status, l.BlockedReason, l.StartDate, l.EndDate,
                l.MaxEnrollments, l.CreatedAt))
            .ToListAsync();
    }

    private async Task<string> SaveThumbnailAsync(Guid id, IFormFile file)
    {
        var dir = Path.Combine(env.WebRootPath, _opts.ThumbnailUploadPath, "programs");
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{id}{ext}";
        await using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/{_opts.ThumbnailUploadPath}/programs/{fileName}";
    }
}
