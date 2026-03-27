using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Videos;

[Authorize(Roles = "Admin,Instructor")]
public class EditModel(IVideoService svc, IDomainService domainSvc, IVideoCategoryService catSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }

    [BindProperty] public int DomainId { get; set; }
    [BindProperty] public int CategoryId { get; set; }
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public int SortOrder { get; set; }
    [BindProperty] public VideoStatus Status { get; set; } = VideoStatus.Draft;
    [BindProperty] public bool IsFreePreview { get; set; }

    public bool IsEdit => Id.HasValue && Id != Guid.Empty;
    public Guid VideoId => Id ?? Guid.Empty;
    public string? FilePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? OriginalFileName { get; set; }
    public string FileSizeMb => FileSizeBytes > 0 ? (FileSizeBytes / 1_048_576.0).ToString("F1") : "0";
    public long FileSizeBytes { get; set; }

    public List<DomainDto> Domains { get; set; } = [];
    public List<VideoCategoryDto> Categories { get; set; } = [];

    private async Task LoadLookupsAsync()
    {
        Domains = await domainSvc.GetAllAsync(activeOnly: false);
        Categories = await catSvc.GetAllAsync(activeOnly: false);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();
        if (IsEdit)
        {
            try
            {
                var v = await svc.GetByIdAsync(Id!.Value);
                DomainId = v.DomainId; CategoryId = v.CategoryId; Title = v.Title;
                Description = v.Description; SortOrder = v.SortOrder; Status = v.Status;
                IsFreePreview = v.IsFreePreview; FilePath = v.FilePath;
                ThumbnailPath = v.ThumbnailPath; OriginalFileName = v.OriginalFileName;
                FileSizeBytes = v.FileSizeBytes;
            }
            catch { return NotFound(); }
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? VideoFile, IFormFile? ThumbnailFile)
    {
        await LoadLookupsAsync();
        if (!ModelState.IsValid) return Page();

        try
        {
            if (IsEdit)
            {
                var req = new UpdateVideoRequest(CategoryId, Title, Description, IsFreePreview, SortOrder, Status);
                await svc.UpdateAsync(Id!.Value, req, ThumbnailFile);
                TempData["Success"] = "Video updated.";
            }
            else
            {
                var req = new CreateVideoRequest(DomainId, CategoryId, Title, Description, IsFreePreview, SortOrder, Status);
                await svc.CreateAsync(req, VideoFile, ThumbnailFile);
                TempData["Success"] = "Video created.";
            }
            return RedirectToPage("Index");
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; return Page(); }
    }
}
