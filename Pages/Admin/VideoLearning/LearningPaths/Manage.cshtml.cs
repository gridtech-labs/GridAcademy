using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.LearningPaths;

/// <summary>
/// Superseded by Builder.cshtml — redirects to Builder.
/// Kept to avoid broken links from old bookmarks.
/// </summary>
[Authorize(Roles = "Admin,Instructor")]
public class ManageModel : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }

    public IActionResult OnGet() =>
        RedirectToPage("Builder", new { id = Id });
}
