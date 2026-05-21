using GridAcademy.Common;
using GridAcademy.DTOs.Users;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Users;

[Authorize(Roles = "Admin")]   // SuperAdmin inherits Admin via IClaimsTransformation
public class IndexModel : PageModel
{
    private readonly IUserService _users;

    public IndexModel(IUserService users) => _users = users;

    public PagedResult<UserDto> Users { get; set; } = new();

    [BindProperty(SupportsGet = true)] public string? Search      { get; set; }
    [BindProperty(SupportsGet = true)] public string? Role        { get; set; }
    [BindProperty(SupportsGet = true)] public string? IsActive    { get; set; }  // "true" | "false" | null
    [BindProperty(SupportsGet = true)] public int     CurrentPage { get; set; } = 1;

    // Whether the current session is a SuperAdmin (controls cross-client visibility)
    public bool IsSuperAdmin => User.IsInRole("SuperAdmin");

    private const int PageSize = 15;

    public async Task OnGetAsync()
    {
        bool? activeFilter = IsActive switch
        {
            "true"  => true,
            "false" => false,
            _       => null
        };

        // SuperAdmin sees all clients; Admin/Instructor see only their own client
        int? clientIdFilter = null;
        if (!IsSuperAdmin)
        {
            var cidClaim = User.FindFirst("ClientId")?.Value;
            if (int.TryParse(cidClaim, out var cid))
                clientIdFilter = cid;
        }

        Users = await _users.GetUsersAsync(new UserListRequest
        {
            Search   = Search,
            Role     = Role,
            IsActive = activeFilter,
            ClientId = clientIdFilter,
            Page     = CurrentPage,
            PageSize = PageSize
        });
    }
}
