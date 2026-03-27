using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridAcademy.Services.VideoLearning;

public class VideoService(AppDbContext db, IWebHostEnvironment env,
    IOptions<VideoLearningStorageOptions> opts) : IVideoService
{
    private readonly VideoLearningStorageOptions _opts = opts.Value;

    private static VideoDto Map(VlVideo v) => new(
        v.Id, v.DomainId, v.Domain?.Name ?? "", v.CategoryId, v.Category?.Name ?? "",
        v.Title, v.Description, v.FilePath, v.ThumbnailPath,
        v.DurationSeconds, v.IsFreePreview, v.Status,
        v.SortOrder, v.FileSizeBytes, v.OriginalFileName,
        v.CreatedAt, v.UpdatedAt);

    public async Task<PagedResult<VideoDto>> GetVideosAsync(VideoListRequest request)
    {
        var q = db.VlVideos.Include(v => v.Domain).Include(v => v.Category).AsQueryable();
        if (request.DomainId.HasValue)   q = q.Where(v => v.DomainId == request.DomainId.Value);
        if (request.CategoryId.HasValue) q = q.Where(v => v.CategoryId == request.CategoryId.Value);
        if (request.Status.HasValue)     q = q.Where(v => v.Status == request.Status.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
            q = q.Where(v => v.Title.Contains(request.Search));

        var total = await q.CountAsync();
        var items = await q.OrderBy(v => v.DomainId).ThenBy(v => v.SortOrder).ThenBy(v => v.Title)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(v => Map(v)).ToListAsync();

        return new PagedResult<VideoDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<VideoDto> GetByIdAsync(Guid id)
    {
        var v = await db.VlVideos.Include(x => x.Domain).Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Video {id} not found.");
        return Map(v);
    }

    public async Task<VideoDto> CreateAsync(CreateVideoRequest request, IFormFile? videoFile, IFormFile? thumbnail, Guid? createdBy = null)
    {
        var entity = new VlVideo
        {
            DomainId = request.DomainId, CategoryId = request.CategoryId,
            Title = request.Title, Description = request.Description,
            IsFreePreview = request.IsFreePreview, SortOrder = request.SortOrder,
            Status = request.Status, CreatedBy = createdBy, UpdatedBy = createdBy,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        if (videoFile != null) await SaveVideoFileAsync(entity, videoFile);
        if (thumbnail  != null) await SaveThumbnailAsync(entity, thumbnail);

        db.VlVideos.Add(entity);
        await db.SaveChangesAsync();
        await db.Entry(entity).Reference(x => x.Domain).LoadAsync();
        await db.Entry(entity).Reference(x => x.Category).LoadAsync();
        return Map(entity);
    }

    public async Task<VideoDto> UpdateAsync(Guid id, UpdateVideoRequest request, IFormFile? thumbnail, Guid? updatedBy = null)
    {
        var entity = await db.VlVideos.Include(x => x.Domain).Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Video {id} not found.");

        entity.CategoryId = request.CategoryId;
        entity.Title      = request.Title;
        entity.Description = request.Description;
        entity.IsFreePreview = request.IsFreePreview;
        entity.SortOrder   = request.SortOrder;
        entity.Status      = request.Status;
        entity.UpdatedBy   = updatedBy;
        entity.UpdatedAt   = DateTime.UtcNow;

        if (thumbnail != null) await SaveThumbnailAsync(entity, thumbnail);

        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task ReplaceVideoFileAsync(Guid id, IFormFile videoFile)
    {
        var entity = await db.VlVideos.FindAsync(id)
            ?? throw new KeyNotFoundException($"Video {id} not found.");
        await SaveVideoFileAsync(entity, videoFile);
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task PublishAsync(Guid id)
    {
        var entity = await db.VlVideos.FindAsync(id)
            ?? throw new KeyNotFoundException($"Video {id} not found.");
        entity.Status    = VideoStatus.Published;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task UnpublishAsync(Guid id)
    {
        var entity = await db.VlVideos.FindAsync(id)
            ?? throw new KeyNotFoundException($"Video {id} not found.");
        entity.Status    = VideoStatus.Draft;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await db.VlVideos.FindAsync(id)
            ?? throw new KeyNotFoundException($"Video {id} not found.");
        // Delete physical files
        DeleteFileIfExists(entity.FilePath);
        DeleteFileIfExists(entity.ThumbnailPath);
        db.VlVideos.Remove(entity);
        await db.SaveChangesAsync();
    }

    // ── Private helpers ─────────────────────────────────────────

    private async Task SaveVideoFileAsync(VlVideo entity, IFormFile file)
    {
        var dir = Path.Combine(env.WebRootPath, _opts.VideoUploadPath, entity.Id.ToString());
        Directory.CreateDirectory(dir);
        var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"video{ext}";
        var fullPath = Path.Combine(dir, fileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);
        entity.FilePath          = $"/{_opts.VideoUploadPath}/{entity.Id}/{fileName}";
        entity.FileSizeBytes     = file.Length;
        entity.OriginalFileName  = file.FileName;
    }

    private async Task SaveThumbnailAsync(VlVideo entity, IFormFile file)
    {
        var dir = Path.Combine(env.WebRootPath, _opts.ThumbnailUploadPath);
        Directory.CreateDirectory(dir);
        var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{entity.Id}{ext}";
        var fullPath = Path.Combine(dir, fileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);
        entity.ThumbnailPath = $"/{_opts.ThumbnailUploadPath}/{fileName}";
    }

    private void DeleteFileIfExists(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var full = Path.Combine(env.WebRootPath, relativePath.TrimStart('/'));
        if (File.Exists(full)) File.Delete(full);
    }
}
