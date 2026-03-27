using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridAcademy.Services.VideoLearning;

public class ContentFileService(AppDbContext db, IWebHostEnvironment env,
    IOptions<VideoLearningStorageOptions> opts) : IContentFileService
{
    private readonly VideoLearningStorageOptions _opts = opts.Value;

    private static ContentFileDto Map(VlContentFile f) => new(
        f.Id, f.DomainId, f.Domain?.Name ?? "", f.Title, f.Description,
        f.ContentType, f.FilePath, f.OriginalFileName, f.FileSizeBytes, f.IsActive, f.CreatedAt);

    public async Task<PagedResult<ContentFileDto>> GetFilesAsync(ContentFileListRequest request)
    {
        var q = db.VlContentFiles.Include(f => f.Domain).AsQueryable();
        if (request.DomainId.HasValue)      q = q.Where(f => f.DomainId == request.DomainId.Value);
        if (request.ContentType.HasValue)   q = q.Where(f => f.ContentType == request.ContentType.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
            q = q.Where(f => f.Title.Contains(request.Search));

        var total = await q.CountAsync();
        var items = (await q.OrderByDescending(f => f.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync())
            .Select(Map).ToList();
        return new PagedResult<ContentFileDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<ContentFileDto> GetByIdAsync(Guid id)
    {
        var f = await db.VlContentFiles.Include(x => x.Domain).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Content file {id} not found.");
        return Map(f);
    }

    public async Task<ContentFileDto> CreateAsync(CreateContentFileRequest request, IFormFile? file, Guid? createdBy = null)
    {
        var entity = new VlContentFile
        {
            DomainId = request.DomainId, Title = request.Title, Description = request.Description,
            ContentType = request.ContentType, IsActive = request.IsActive,
            CreatedBy = createdBy, UpdatedBy = createdBy,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        if (file != null) await SaveFileAsync(entity, file);
        db.VlContentFiles.Add(entity);
        await db.SaveChangesAsync();
        await db.Entry(entity).Reference(x => x.Domain).LoadAsync();
        return Map(entity);
    }

    public async Task<ContentFileDto> UpdateAsync(Guid id, CreateContentFileRequest request, IFormFile? file = null, Guid? updatedBy = null)
    {
        var entity = await db.VlContentFiles.Include(x => x.Domain).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Content file {id} not found.");
        entity.DomainId = request.DomainId; entity.Title = request.Title;
        entity.Description = request.Description; entity.ContentType = request.ContentType;
        entity.IsActive = request.IsActive; entity.UpdatedBy = updatedBy; entity.UpdatedAt = DateTime.UtcNow;
        if (file != null) await SaveFileAsync(entity, file);
        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await db.VlContentFiles.FindAsync(id)
            ?? throw new KeyNotFoundException($"Content file {id} not found.");
        if (!string.IsNullOrWhiteSpace(entity.FilePath))
        {
            var full = Path.Combine(env.WebRootPath, entity.FilePath.TrimStart('/'));
            if (File.Exists(full)) File.Delete(full);
        }
        db.VlContentFiles.Remove(entity);
        await db.SaveChangesAsync();
    }

    private async Task SaveFileAsync(VlContentFile entity, IFormFile file)
    {
        var subDir = entity.ContentType switch {
            ContentFileType.Scorm => "scorm",
            ContentFileType.Pdf   => "pdf",
            ContentFileType.Html  => "html",
            _ => "files"
        };
        var dir = Path.Combine(env.WebRootPath, "uploads", subDir, entity.Id.ToString());
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"content{ext}";
        var fullPath = Path.Combine(dir, fileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);
        entity.FilePath = $"/uploads/{subDir}/{entity.Id}/{fileName}";
        entity.FileSizeBytes = file.Length;
        entity.OriginalFileName = file.FileName;
    }
}
