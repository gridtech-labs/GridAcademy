using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.LearningPaths;

[Authorize(Roles = "Admin,Instructor")]
public class BuilderModel(ILearningPathService lpSvc, IDomainService domainSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }

    public LearningPathDetailDto? Detail { get; set; }
    public List<DomainDto> Domains { get; set; } = [];

    public bool IsNew => Id == Guid.Empty;

    public static string NodeIcon(string nodeType) => nodeType switch {
        "VL" => "bi-camera-video-fill",
        "AS" => "bi-journal-check",
        "SC" => "bi-box-seam-fill",
        "PD" => "bi-file-earmark-pdf-fill",
        "HT" => "bi-code-slash",
        _    => "bi-file-earmark"
    };

    public async Task<IActionResult> OnGetAsync()
    {
        Domains = await domainSvc.GetAllAsync();
        if (!IsNew)
        {
            try { Detail = await lpSvc.GetDetailAsync(Id); }
            catch { return NotFound(); }
        }
        return Page();
    }

    // ── Add Module ─────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddModuleAsync(string ModuleTitle)
    {
        if (!string.IsNullOrWhiteSpace(ModuleTitle))
        {
            await lpSvc.AddModuleAsync(Id, new CreateLpModuleRequest(ModuleTitle.Trim()));
            TempData["Success"] = $"Module \"{ModuleTitle}\" added.";
        }
        return RedirectToPage(new { id = Id });
    }

    // ── Add Content (batch — multi-select) ─────────────────────────────────
    /// <summary>
    /// Handles adding one or more content nodes under an optional parent module.
    /// ContentIds is a comma-separated list of Guids.
    /// </summary>
    public async Task<IActionResult> OnPostAddContentAsync(
        int? ParentNodeId, string NodeType, string ContentIds, bool IsPreview = false)
    {
        if (!string.IsNullOrWhiteSpace(ContentIds) && !string.IsNullOrWhiteSpace(NodeType))
        {
            var ids = ContentIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue).Select(g => g!.Value).ToList();

            if (ids.Count > 0)
            {
                await lpSvc.AddContentBatchAsync(Id, new AddLpContentBatchRequest(
                    ParentNodeId, NodeType, ids, IsPreview));
                TempData["Success"] = $"{ids.Count} content item(s) added.";
            }
        }
        return RedirectToPage(new { id = Id });
    }

    // ── Delete Node (module or content) ────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteNodeAsync(int NodeId)
    {
        try { await lpSvc.DeleteNodeAsync(NodeId); TempData["Success"] = "Node removed."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage(new { id = Id });
    }
}
