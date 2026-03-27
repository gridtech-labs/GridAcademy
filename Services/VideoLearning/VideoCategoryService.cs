using GridAcademy.Data;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.VideoLearning;

public class VideoCategoryService(AppDbContext db) : IVideoCategoryService
{
    private static VideoCategoryDto Map(VlVideoCategory c, int videoCount) =>
        new(c.Id, c.DomainId, c.Domain?.Name ?? "", c.Name, c.IsActive, c.SortOrder, videoCount);

    public async Task<List<VideoCategoryDto>> GetAllAsync(bool activeOnly = true)
    {
        var q = db.VlVideoCategories.Include(c => c.Domain).AsQueryable();
        if (activeOnly) q = q.Where(c => c.IsActive);
        var list = await q.OrderBy(c => c.DomainId).ThenBy(c => c.SortOrder).ToListAsync();
        var counts = await db.VlVideos.GroupBy(v => v.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return list.Select(c => Map(c, counts.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<List<VideoCategoryDto>> GetByDomainAsync(int domainId, bool activeOnly = true)
    {
        var q = db.VlVideoCategories.Include(c => c.Domain)
            .Where(c => c.DomainId == domainId);
        if (activeOnly) q = q.Where(c => c.IsActive);
        var list = await q.OrderBy(c => c.SortOrder).ToListAsync();
        var counts = await db.VlVideos.Where(v => v.DomainId == domainId)
            .GroupBy(v => v.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return list.Select(c => Map(c, counts.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<VideoCategoryDto> GetByIdAsync(int id)
    {
        var c = await db.VlVideoCategories.Include(x => x.Domain).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");
        var count = await db.VlVideos.CountAsync(v => v.CategoryId == id);
        return Map(c, count);
    }

    public async Task<VideoCategoryDto> CreateAsync(CreateVideoCategoryRequest request)
    {
        var entity = new VlVideoCategory
        {
            DomainId = request.DomainId, Name = request.Name,
            SortOrder = request.SortOrder, IsActive = request.IsActive
        };
        db.VlVideoCategories.Add(entity);
        await db.SaveChangesAsync();
        await db.Entry(entity).Reference(x => x.Domain).LoadAsync();
        return Map(entity, 0);
    }

    public async Task<VideoCategoryDto> UpdateAsync(int id, CreateVideoCategoryRequest request)
    {
        var entity = await db.VlVideoCategories.Include(x => x.Domain).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");
        entity.DomainId = request.DomainId;
        entity.Name = request.Name;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        var count = await db.VlVideos.CountAsync(v => v.CategoryId == id);
        return Map(entity, count);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.VlVideoCategories.FindAsync(id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");
        if (await db.VlVideos.AnyAsync(v => v.CategoryId == id))
            throw new InvalidOperationException("Cannot delete category with existing videos.");
        db.VlVideoCategories.Remove(entity);
        await db.SaveChangesAsync();
    }
}
