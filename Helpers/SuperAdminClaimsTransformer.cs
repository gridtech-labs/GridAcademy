using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace GridAcademy.Helpers;

/// <summary>
/// Adds "Admin" and "Instructor" role claims to SuperAdmin users so they automatically
/// pass all existing [Authorize(Roles = "Admin")] and [Authorize(Roles = "Admin,Instructor")]
/// checks throughout the admin panel without requiring every page to be updated.
/// </summary>
public class SuperAdminClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only enrich if the user is truly SuperAdmin and hasn't been enriched yet
        if (!principal.IsInRole("SuperAdmin") || principal.IsInRole("Admin"))
            return Task.FromResult(principal);

        // Clone the first identity (preserves all original claims)
        var original = principal.Identities.FirstOrDefault();
        if (original is null) return Task.FromResult(principal);

        var enriched = new ClaimsIdentity(original);
        enriched.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        enriched.AddClaim(new Claim(ClaimTypes.Role, "Instructor"));

        return Task.FromResult(new ClaimsPrincipal(enriched));
    }
}
