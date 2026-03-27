using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Programs;

[Authorize(Roles = "Admin,Instructor")]
public class WizardModel(IProgramService programSvc, IDomainService domainSvc,
    ILearningPathService lpSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true)] public int CurrentStep { get; set; } = 1;

    // Step 1 fields
    [BindProperty] public string ProgramTitle { get; set; } = "";
    [BindProperty] public string? LearningCode { get; set; }
    [BindProperty] public int DomainId { get; set; }
    [BindProperty] public string? ShortDescription { get; set; }
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public bool IsBlendedLearning { get; set; }

    public bool IsEdit => Id != Guid.Empty;
    public List<DomainDto> Domains { get; set; } = [];
    public List<LearningPathDto> AssignedPaths  { get; set; } = [];
    public List<LearningPathDto> AvailablePaths { get; set; } = [];
    public List<CourseLaunchDto> CourseLaunches { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        Domains = await domainSvc.GetAllAsync();
        if (IsEdit)
        {
            try
            {
                var prog = await programSvc.GetByIdAsync(Id);
                ProgramTitle = prog.Title;
                DomainId = prog.DomainId; ShortDescription = prog.ShortDescription;
                Description = prog.Description;
                AssignedPaths = prog.LearningPaths.ToList();
                var all = await lpSvc.GetAllAsync(activeOnly: false);
                var ids = AssignedPaths.Select(x => x.Id).ToHashSet();
                AvailablePaths = all.Where(x => !ids.Contains(x.Id)).ToList();
                CourseLaunches = await programSvc.GetCourseLaunchesAsync(Id);
            }
            catch { return NotFound(); }
        }
        return Page();
    }

    public async Task<IActionResult> OnPostSaveBasicAsync()
    {
        Domains = await domainSvc.GetAllAsync();
        try
        {
            if (IsEdit)
            {
                await programSvc.UpdateAsync(Id, new UpdateProgramRequest(
                    DomainId, ProgramTitle, LearningCode, ShortDescription, Description,
                    IsBlendedLearning, ProgramStatus.Draft));
                TempData["Success"] = "Program updated.";
            }
            else
            {
                var prog = await programSvc.CreateAsync(new CreateProgramRequest(
                    DomainId, ProgramTitle, LearningCode, ShortDescription, Description,
                    IsBlendedLearning));
                Id = prog.Id;
                TempData["Success"] = "Program created.";
            }
            return RedirectToPage(new { id = Id, step = 2 });
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; return Page(); }
    }

    public async Task<IActionResult> OnPostAddLpAsync(Guid LpId)
    {
        if (LpId != Guid.Empty) await programSvc.AddLearningPathAsync(Id, LpId);
        TempData["Success"] = "Learning path added.";
        return RedirectToPage(new { id = Id, step = 2 });
    }

    public async Task<IActionResult> OnPostRemoveLpAsync(Guid lpId)
    {
        await programSvc.RemoveLearningPathAsync(Id, lpId);
        TempData["Success"] = "Learning path removed.";
        return RedirectToPage(new { id = Id, step = 2 });
    }

    public async Task<IActionResult> OnPostSaveLaunchAsync(
        int LaunchId, string LaunchName, int LaunchStatus,
        string? BlockedReason, DateTime? LaunchStartDate, DateTime? LaunchEndDate, int MaxEnrollments)
    {
        var status = (CourseLaunchStatus)LaunchStatus;
        var request = new CreateCourseLaunchRequest(LaunchName, status, BlockedReason,
            LaunchStartDate, LaunchEndDate, MaxEnrollments);
        if (LaunchId > 0) await programSvc.UpdateCourseLaunchAsync(LaunchId, request);
        else              await programSvc.AddCourseLaunchAsync(Id, request);
        TempData["Success"] = "Launch saved.";
        return RedirectToPage(new { id = Id, step = 3 });
    }

    public async Task<IActionResult> OnPostDeleteLaunchAsync(int launchId)
    {
        try { await programSvc.DeleteCourseLaunchAsync(launchId); TempData["Success"] = "Launch deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage(new { id = Id, step = 3 });
    }
}
