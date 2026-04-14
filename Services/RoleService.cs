using GridAcademy.Data;
using GridAcademy.Data.Entities;
using GridAcademy.DTOs.Users;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext          _db;
    private readonly ILogger<RoleService> _logger;

    public RoleService(AppDbContext db, ILogger<RoleService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<List<SystemRoleDto>> GetRolesAsync()
    {
        var roles = await _db.SystemRoles
            .AsNoTracking()
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync();

        // User count per role (one query)
        var counts = await _db.UserRoleMaps
            .GroupBy(m => m.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count);

        return roles.Select(r => MapToDto(r, counts.GetValueOrDefault(r.Id, 0))).ToList();
    }

    public async Task<SystemRoleDto> GetByIdAsync(int id)
    {
        var role = await _db.SystemRoles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Role {id} not found.");

        var count = await _db.UserRoleMaps.CountAsync(m => m.RoleId == id);
        return MapToDto(role, count);
    }

    public async Task<SystemRoleDto> CreateAsync(CreateSystemRoleRequest request)
    {
        var name = request.Name.Trim();
        if (await _db.SystemRoles.AnyAsync(r => r.Name == name))
            throw new InvalidOperationException($"A role named '{name}' already exists.");

        var role = new SystemRole
        {
            Name        = name,
            DisplayName = request.DisplayName.Trim(),
            Description = request.Description?.Trim(),
            Color       = request.Color,
            IsSystem    = false,
            IsActive    = request.IsActive,
            SortOrder   = request.SortOrder,
            CreatedAt   = DateTime.UtcNow
        };

        _db.SystemRoles.Add(role);
        await _db.SaveChangesAsync();
        _logger.LogInformation("System role created: {Name}", role.Name);
        return MapToDto(role, 0);
    }

    public async Task<SystemRoleDto> UpdateAsync(int id, CreateSystemRoleRequest request)
    {
        var role = await _db.SystemRoles.FindAsync(id)
            ?? throw new KeyNotFoundException($"Role {id} not found.");

        var name = request.Name.Trim();
        if (await _db.SystemRoles.AnyAsync(r => r.Name == name && r.Id != id))
            throw new InvalidOperationException($"A role named '{name}' already exists.");

        // System roles: only allow Description, DisplayName, Color, SortOrder to change
        if (!role.IsSystem)
            role.Name = name;

        role.DisplayName = request.DisplayName.Trim();
        role.Description = request.Description?.Trim();
        role.Color       = request.Color;
        role.IsActive    = request.IsActive;
        role.SortOrder   = request.SortOrder;

        await _db.SaveChangesAsync();
        _logger.LogInformation("System role updated: {Id}", id);

        var count = await _db.UserRoleMaps.CountAsync(m => m.RoleId == id);
        return MapToDto(role, count);
    }

    public async Task ToggleActiveAsync(int id)
    {
        var role = await _db.SystemRoles.FindAsync(id)
            ?? throw new KeyNotFoundException($"Role {id} not found.");

        if (role.IsSystem)
            throw new InvalidOperationException("System roles cannot be deactivated.");

        role.IsActive = !role.IsActive;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var role = await _db.SystemRoles.FindAsync(id)
            ?? throw new KeyNotFoundException($"Role {id} not found.");

        if (role.IsSystem)
            throw new InvalidOperationException("System roles cannot be deleted.");

        var hasUsers = await _db.UserRoleMaps.AnyAsync(m => m.RoleId == id);
        if (hasUsers)
            throw new InvalidOperationException(
                "Cannot delete a role that has users assigned to it. Re-assign users first.");

        _db.SystemRoles.Remove(role);
        await _db.SaveChangesAsync();
        _logger.LogInformation("System role deleted: {Id}", id);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static SystemRoleDto MapToDto(SystemRole r, int userCount) => new()
    {
        Id          = r.Id,
        Name        = r.Name,
        DisplayName = r.DisplayName,
        Description = r.Description,
        Color       = r.Color,
        IsSystem    = r.IsSystem,
        IsActive    = r.IsActive,
        SortOrder   = r.SortOrder,
        UserCount   = userCount,
        CreatedAt   = r.CreatedAt
    };
}
