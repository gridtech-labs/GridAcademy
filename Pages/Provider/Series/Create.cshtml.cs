using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Marketplace;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Provider.Series;

[Authorize(Roles = "Provider")]
public class CreateModel : PageModel
{
    private readonly IProviderService _providers;
    private readonly AppDbContext     _db;

    public CreateModel(IProviderService providers, AppDbContext db)
    {
        _providers = providers;
        _db        = db;
    }

    [BindProperty]
    public SeriesForm Form { get; set; } = new();

    public List<(int Id, string Name)> ExamTypes { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken ct = default)
    {
        await LoadExamTypesAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct = default)
    {
        await LoadExamTypesAsync(ct);

        if (!ModelState.IsValid) return Page();

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var series = await _providers.CreateSeriesAsync(userId, new CreateTestSeriesRequest(
                Form.Title,
                Form.ExamTypeId,
                Form.SeriesType,
                Form.ShortDescription,
                Form.FullDescription,
                Form.ThumbnailUrl,
                Form.PriceInr,
                Form.IsFirstTestFree,
                Form.Language
            ), ct);

            TempData["Success"] = "Series created successfully. Now add tests to your series.";
            return RedirectToPage("/Provider/Series/Edit", new { id = series.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return Page();
        }
    }

    private async Task LoadExamTypesAsync(CancellationToken ct)
    {
        ExamTypes = await _db.ExamTypes
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Select(e => new { e.Id, e.Name })
            .ToListAsync(ct)
            .ContinueWith(t => t.Result.Select(x => (x.Id, x.Name)).ToList(), ct);
    }

    public class SeriesForm
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Exam type is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select an exam type.")]
        [Display(Name = "Exam Type")]
        public int ExamTypeId { get; set; }

        [Display(Name = "Series Type")]
        public SeriesType SeriesType { get; set; } = SeriesType.FullMock;

        [MaxLength(500)]
        [Display(Name = "Short Description")]
        public string? ShortDescription { get; set; }

        [Display(Name = "Full Description")]
        public string? FullDescription { get; set; }

        [MaxLength(500)]
        [Display(Name = "Thumbnail URL")]
        public string? ThumbnailUrl { get; set; }

        [Range(0, 100000)]
        [Display(Name = "Price (INR)")]
        public decimal PriceInr { get; set; } = 0;

        [Display(Name = "Is First Test Free")]
        public bool IsFirstTestFree { get; set; } = false;

        [MaxLength(20)]
        public string Language { get; set; } = "English";
    }
}
