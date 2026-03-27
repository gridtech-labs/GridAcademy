using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Programs;

[Authorize(Roles = "Admin,Instructor")]
public class EditModel(IProgramService svc, IDomainService domainSvc, ILearningPathService lpSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }

    [BindProperty] public int DomainId { get; set; }
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? ShortDescription { get; set; }
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public ProgramStatus Status { get; set; } = ProgramStatus.Draft;

    public bool IsEdit => Id.HasValue && Id != Guid.Empty;
    public Guid ProgramId => Id ?? Guid.Empty;
    public string? ThumbnailPath { get; set; }

    public List<DomainDto> Domains { get; set; } = [];
    public ProgramDto? ProgramData { get; set; }
    public List<LearningPathDto> AssignedPaths { get; set; } = [];
    public List<LearningPathDto> AvailablePaths { get; set; } = [];

    private async Task LoadLookupsAsync()
    {
        Domains = await domainSvc.GetAllAsync(activeOnly: false);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();
        if (IsEdit)
        {
            try
            {
                ProgramData = await svc.GetByIdAsync(Id!.Value);
                DomainId = ProgramData.DomainId; Title = ProgramData.Title;
                ShortDescription = ProgramData.ShortDescription; Description = ProgramData.Description;
                Status = ProgramData.Status; ThumbnailPath = ProgramData.ThumbnailPath;
                AssignedPaths = ProgramData.LearningPaths.ToList();
                var allPaths = await lpSvc.GetAllAsync(activeOnly: false);
                var assignedIds = AssignedPaths.Select(lp => lp.Id).ToHashSet();
                AvailablePaths = allPaths.Where(lp => !assignedIds.Contains(lp.Id)).ToList();
            }
            catch { return NotFound(); }
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? ThumbnailFile)
    {
        await LoadLookupsAsync();
        if (!ModelState.IsValid) return Page();

        try
        {
            if (IsEdit)
            {
                var req = new UpdateProgramRequest(DomainId, Title, null, ShortDescription, Description, false, Status);
                await svc.UpdateAsync(Id!.Value, req, ThumbnailFile);
                TempData["Success"] = "Program updated.";
                return RedirectToPage();
            }
            else
            {
                var req = new CreateProgramRequest(DomainId, Title, null, ShortDescription, Description, false, Status);
                var created = await svc.CreateAsync(req, ThumbnailFile);
                TempData["Success"] = "Program created.";
                return RedirectToPage(new { id = created.Id });
            }
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; return Page(); }
    }

    public async Task<IActionResult> OnPostAddPlanAsync(string planName, decimal priceInr, decimal priceUsd, int? validityDays)
    {
        try
        {
            var req = new CreatePricingPlanRequest(planName, priceInr, priceUsd, null, null, validityDays);
            await svc.AddPricingPlanAsync(Id!.Value, req);
            TempData["Success"] = "Pricing plan added.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeletePlanAsync(int planId)
    {
        try { await svc.DeletePricingPlanAsync(planId); TempData["Success"] = "Plan deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddLpAsync(Guid LpId)
    {
        if (LpId != Guid.Empty)
            await svc.AddLearningPathAsync(Id!.Value, LpId);
        TempData["Success"] = "Learning path added.";
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveLpAsync(Guid LpId)
    {
        await svc.RemoveLearningPathAsync(Id!.Value, LpId);
        TempData["Success"] = "Learning path removed.";
        return RedirectToPage(new { id = Id });
    }
}
