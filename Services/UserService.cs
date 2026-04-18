using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities;
using GridAcademy.DTOs.Users;
using GridAcademy.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GridAcademy.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserService> _logger;

    // Fallback valid roles used only when system_roles table is empty (e.g. first boot before seeding).
    // After seeding, validation is done against the DB.
    private static readonly HashSet<string> FallbackRoles = ["Admin", "Instructor", "User", "Student", "Provider"];

    public UserService(AppDbContext db, ILogger<UserService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── List / Search ────────────────────────────────────────────────────────
    public async Task<PagedResult<UserDto>> GetUsersAsync(UserListRequest request)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        // Optional filters
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term)  ||
                u.Email.ToLower().Contains(term)     ||
                (u.Phone != null && u.Phone.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
            query = query.Where(u => u.Role == request.Role);

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => MapToDto(u))
            .ToListAsync();

        return new PagedResult<UserDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = request.Page,
            PageSize   = request.PageSize
        };
    }

    // ── Get Single ──────────────────────────────────────────────────────────
    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        return MapToDto(user);
    }

    // ── Create ──────────────────────────────────────────────────────────────
    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        // Validate role against DB (fallback to hardcoded list if roles table not seeded yet)
        var role = request.Role.Trim();
        await ValidateRoleAsync(role);

        // Validate password strength
        var pwError = PasswordHelper.Validate(request.Password);
        if (pwError is not null) throw new ArgumentException(pwError);

        // Check email uniqueness
        var emailTaken = await _db.Users
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower().Trim());

        if (emailTaken)
            throw new ArgumentException($"A user with email '{request.Email}' already exists.");

        var user = new User
        {
            FirstName    = request.FirstName.Trim(),
            LastName     = request.LastName.Trim(),
            Email        = request.Email.Trim().ToLower(),
            PasswordHash = PasswordHelper.Hash(request.Password),
            Role         = role,
            IsActive     = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Map to system role table
        await SyncUserRoleMapAsync(user.Id, role, assignedBy: null);

        _logger.LogInformation("User created: {Email} ({Role})", user.Email, user.Role);
        return MapToDto(user);
    }

    // ── Update ──────────────────────────────────────────────────────────────
    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        var role = request.Role.Trim();
        await ValidateRoleAsync(role);

        user.FirstName = request.FirstName.Trim();
        user.LastName  = request.LastName.Trim();
        user.Role      = role;
        user.IsActive  = request.IsActive;
        // UpdatedAt is stamped automatically by AppDbContext.SaveChangesAsync()

        await _db.SaveChangesAsync();

        // Keep user_role_maps in sync when role changes
        await SyncUserRoleMapAsync(user.Id, role, assignedBy: null);

        _logger.LogInformation("User updated: {Id}", id);
        return MapToDto(user);
    }

    // ── Delete ──────────────────────────────────────────────────────────────
    public async Task DeleteAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        _logger.LogInformation("User deleted: {Id}", id);
    }

    // ── Role validation ──────────────────────────────────────────────────────
    private async Task ValidateRoleAsync(string role)
    {
        // Try DB first; fall back to hardcoded list during first-boot before seeder runs
        var dbRoles = await _db.SystemRoles
            .AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => r.Name)
            .ToListAsync();

        var valid = dbRoles.Count > 0 ? dbRoles.ToHashSet() : FallbackRoles;

        if (!valid.Contains(role))
            throw new ArgumentException(
                $"Invalid role '{role}'. Allowed: {string.Join(", ", valid.OrderBy(r => r))}");
    }

    // ── Role map sync ────────────────────────────────────────────────────────
    /// <summary>
    /// Ensures user_role_maps has exactly one record for this user pointing to the
    /// SystemRole whose Name matches <paramref name="roleName"/>.
    /// Silently no-ops if the SystemRole table hasn't been seeded yet.
    /// </summary>
    private async Task SyncUserRoleMapAsync(Guid userId, string roleName, Guid? assignedBy)
    {
        var systemRole = await _db.SystemRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == roleName);

        if (systemRole is null) return; // roles not seeded yet — skip

        // Remove any existing mappings for this user (one user → one primary role)
        var existing = await _db.UserRoleMaps
            .Where(m => m.UserId == userId)
            .ToListAsync();

        if (existing.Count == 1 && existing[0].RoleId == systemRole.Id)
            return; // already correct — nothing to do

        _db.UserRoleMaps.RemoveRange(existing);

        _db.UserRoleMaps.Add(new UserRoleMap
        {
            UserId     = userId,
            RoleId     = systemRole.Id,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        });

        await _db.SaveChangesAsync();
    }

    // ── Mapping helper (keeps controllers clean) ────────────────────────────
    private static UserDto MapToDto(User u) => new()
    {
        Id          = u.Id,
        FirstName   = u.FirstName,
        LastName    = u.LastName,
        FullName    = u.FullName,
        Email       = u.Email,
        Phone       = u.Phone,
        Role        = u.Role,
        IsActive    = u.IsActive,
        CreatedAt   = u.CreatedAt,
        LastLoginAt = u.LastLoginAt
    };
}
