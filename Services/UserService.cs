using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities;
using GridAcademy.DTOs.Users;
using GridAcademy.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserService> _logger;

    // Valid role values — Admin manages everything, Instructor manages content, Student/Provider for marketplace
    private static readonly HashSet<string> ValidRoles = ["Admin", "Instructor", "User", "Student", "Provider"];

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
                u.Email.ToLower().Contains(term));
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
        // Validate role
        var role = request.Role.Trim();
        if (!ValidRoles.Contains(role))
            throw new ArgumentException($"Invalid role '{role}'. Allowed: {string.Join(", ", ValidRoles)}");

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

        _logger.LogInformation("User created: {Email} ({Role})", user.Email, user.Role);
        return MapToDto(user);
    }

    // ── Update ──────────────────────────────────────────────────────────────
    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        var role = request.Role.Trim();
        if (!ValidRoles.Contains(role))
            throw new ArgumentException($"Invalid role '{role}'. Allowed: {string.Join(", ", ValidRoles)}");

        user.FirstName = request.FirstName.Trim();
        user.LastName  = request.LastName.Trim();
        user.Role      = role;
        user.IsActive  = request.IsActive;
        // UpdatedAt is stamped automatically by AppDbContext.SaveChangesAsync()

        await _db.SaveChangesAsync();
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

    // ── Mapping helper (keeps controllers clean) ────────────────────────────
    private static UserDto MapToDto(User u) => new()
    {
        Id          = u.Id,
        FirstName   = u.FirstName,
        LastName    = u.LastName,
        FullName    = u.FullName,
        Email       = u.Email,
        Role        = u.Role,
        IsActive    = u.IsActive,
        CreatedAt   = u.CreatedAt,
        LastLoginAt = u.LastLoginAt
    };
}
