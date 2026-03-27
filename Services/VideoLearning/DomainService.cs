using GridAcademy.Data;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.VideoLearning;

public class DomainService(AppDbContext db) : IDomainService
{
    public async Task<List<DomainDto>> GetAllAsync(bool activeOnly = true)
    {
        var q = db.VlDomains.AsQueryable();
        if (activeOnly) q = q.Where(d => d.IsActive);
        return await q.OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new DomainDto(d.Id, d.Name, d.Description, d.LogoUrl, d.IsActive, d.SortOrder,
                d.Videos.Count))
            .ToListAsync();
    }

    public async Task<DomainDto> GetByIdAsync(int id)
    {
        var d = await db.VlDomains.Include(x => x.Videos)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Domain {id} not found.");
        return new DomainDto(d.Id, d.Name, d.Description, d.LogoUrl, d.IsActive, d.SortOrder, d.Videos.Count);
    }

    public async Task<DomainDto> CreateAsync(CreateDomainRequest request)
    {
        var entity = new VlDomain
        {
            Name = request.Name, Description = request.Description,
            SortOrder = request.SortOrder, IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.VlDomains.Add(entity);
        await db.SaveChangesAsync();
        return new DomainDto(entity.Id, entity.Name, entity.Description, entity.LogoUrl, entity.IsActive, entity.SortOrder, 0);
    }

    public async Task<DomainDto> UpdateAsync(int id, CreateDomainRequest request)
    {
        var entity = await db.VlDomains.FindAsync(id)
            ?? throw new KeyNotFoundException($"Domain {id} not found.");
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return new DomainDto(entity.Id, entity.Name, entity.Description, entity.LogoUrl, entity.IsActive, entity.SortOrder,
            await db.VlVideos.CountAsync(v => v.DomainId == id));
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.VlDomains.FindAsync(id)
            ?? throw new KeyNotFoundException($"Domain {id} not found.");
        if (await db.VlVideos.AnyAsync(v => v.DomainId == id))
            throw new InvalidOperationException("Cannot delete domain with existing videos. Deactivate it instead.");
        db.VlDomains.Remove(entity);
        await db.SaveChangesAsync();
    }
}
