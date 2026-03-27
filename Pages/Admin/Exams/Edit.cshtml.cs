using GridAcademy.Data;
using GridAcademy.Data.Entities.Content;
using GridAcademy.Data.Entities.Exam;
using GridAcademy.DTOs.Exam;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.Exams;

[Authorize(Roles = "Admin,Instructor")]
public class EditModel(IExamService svc, AppDbContext db, IWebHostEnvironment env) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }

    [BindProperty] public string  Title            { get; set; } = "";
    [BindProperty] public string  Slug             { get; set; } = "";
    [BindProperty] public string? ShortDescription { get; set; }
    [BindProperty] public string? Overview         { get; set; }
    [BindProperty] public string? Eligibility      { get; set; }
    [BindProperty] public string? Syllabus         { get; set; }
    [BindProperty] public string? ExamPattern      { get; set; }
    [BindProperty] public string? ImportantDates   { get; set; }
    [BindProperty] public string? AdmitCard        { get; set; }
    [BindProperty] public string? ResultInfo       { get; set; }
    [BindProperty] public string? CutOff           { get; set; }
    [BindProperty] public string? HowToApply       { get; set; }
    [BindProperty] public string? ConductingBody   { get; set; }
    [BindProperty] public string? OfficialWebsite  { get; set; }
    [BindProperty] public string? NotificationUrl  { get; set; }
    [BindProperty] public string? ThumbnailUrl     { get; set; }
    [BindProperty] public string? BannerUrl        { get; set; }
    [BindProperty] public int?    ExamLevelId      { get; set; }
    [BindProperty] public int?    ExamTypeId       { get; set; }
    [BindProperty] public bool    IsFeatured       { get; set; }
    [BindProperty] public bool    IsActive         { get; set; } = true;
    [BindProperty] public ExamPageStatus Status    { get; set; } = ExamPageStatus.Draft;
    [BindProperty] public int     SortOrder        { get; set; }
    [BindProperty] public string? MetaTitle        { get; set; }
    [BindProperty] public string? MetaDescription  { get; set; }

    // File uploads (not persisted as bind props — handled explicitly)
    public IFormFile? ThumbnailFile { get; set; }
    public IFormFile? BannerFile    { get; set; }

    public bool               IsEdit    => Id.HasValue && Id != Guid.Empty;
    public List<ExamLevelDto> Levels    { get; set; } = [];
    public List<ExamType>     ExamTypes { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadDropdownsAsync();
        if (IsEdit)
        {
            var dto = await svc.GetExamByIdAsync(Id!.Value);
            if (dto == null) return NotFound();
            Title = dto.Title; Slug = dto.Slug; ShortDescription = dto.ShortDescription;
            Overview = dto.Overview; Eligibility = dto.Eligibility; Syllabus = dto.Syllabus;
            ExamPattern = dto.ExamPattern; ImportantDates = dto.ImportantDates;
            AdmitCard = dto.AdmitCard; ResultInfo = dto.ResultInfo; CutOff = dto.CutOff;
            HowToApply = dto.HowToApply;
            ConductingBody = dto.ConductingBody; OfficialWebsite = dto.OfficialWebsite;
            NotificationUrl = dto.NotificationUrl; ThumbnailUrl = dto.ThumbnailUrl;
            BannerUrl = dto.BannerUrl; IsFeatured = dto.IsFeatured;
            MetaTitle = dto.MetaTitle; MetaDescription = dto.MetaDescription;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadDropdownsAsync();
        if (!ModelState.IsValid) return Page();

        // Handle thumbnail upload if a file was provided
        var thumbFile = Request.Form.Files["ThumbnailFile"];
        if (thumbFile is { Length: > 0 })
            ThumbnailUrl = await SaveUploadAsync(thumbFile, "thumbnails");

        // Handle banner upload if a file was provided
        var bannerFile = Request.Form.Files["BannerFile"];
        if (bannerFile is { Length: > 0 })
            BannerUrl = await SaveUploadAsync(bannerFile, "banners");

        var req = BuildRequest();
        try
        {
            var uid = Guid.TryParse(
                User.FindFirst("sub")?.Value ??
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out var g) ? g : (Guid?)null;
            if (IsEdit)
            {
                await svc.UpdateExamAsync(Id!.Value, req, uid);
                TempData["Success"] = "Exam updated.";
                return RedirectToPage("Tests", new { id = Id });
            }
            else
            {
                var created = await svc.CreateExamAsync(req, uid);
                TempData["Success"] = "Exam created. Now add tests below.";
                return RedirectToPage("Tests", new { id = created.Id });
            }
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; return Page(); }
    }

    /// <summary>CKEditor 5 image upload (JSON response)</summary>
    public async Task<IActionResult> OnPostUploadImageAsync()
    {
        var file = Request.Form.Files["upload"];
        if (file is null or { Length: 0 })
            return BadRequest(new { error = new { message = "No file received." } });
        var url = await SaveUploadAsync(file, "content");
        return new JsonResult(new { url });
    }

    /// <summary>CKEditor 4 image upload — returns HTML script response expected by CKEditor 4 filebrowser</summary>
    public async Task<IActionResult> OnPostCkUploadImageAsync()
    {
        var file = Request.Form.Files.FirstOrDefault();
        var funcNum = Request.Query["CKEditorFuncNum"].FirstOrDefault() ?? "0";
        if (file is null or { Length: 0 })
        {
            var errScript = $"<script>window.parent.CKEDITOR.tools.callFunction({funcNum},'','No file received.')</script>";
            return Content(errScript, "text/html");
        }
        try
        {
            var url = await SaveUploadAsync(file, "content");
            var okScript = $"<script>window.parent.CKEDITOR.tools.callFunction({funcNum},'{url}','')</script>";
            return Content(okScript, "text/html");
        }
        catch (Exception ex)
        {
            var errScript = $"<script>window.parent.CKEDITOR.tools.callFunction({funcNum},'','{ex.Message}')</script>";
            return Content(errScript, "text/html");
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<string> SaveUploadAsync(IFormFile file, string subfolder)
    {
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext)) throw new InvalidOperationException($"File type '{ext}' not allowed.");

        var uploadDir = Path.Combine(env.WebRootPath, "uploads", "exams", subfolder);
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadDir, fileName);
        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);

        return $"/uploads/exams/{subfolder}/{fileName}";
    }

    private SaveExamPageRequest BuildRequest() => new(
        Title, Slug, ShortDescription, Overview, Eligibility, Syllabus,
        ExamPattern, ImportantDates, AdmitCard, ResultInfo, CutOff, HowToApply,
        ConductingBody, OfficialWebsite, NotificationUrl,
        ThumbnailUrl, BannerUrl, ExamLevelId, ExamTypeId,
        IsFeatured, IsActive, Status, SortOrder, MetaTitle, MetaDescription);

    private async Task LoadDropdownsAsync()
    {
        Levels    = await svc.GetExamLevelsAsync();
        ExamTypes = await db.ExamTypes.Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToListAsync();
    }
}
