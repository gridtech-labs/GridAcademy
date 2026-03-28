using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GridAcademy.Data;
using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Provider.Tests;

[Authorize(Roles = "Provider")]
public class CreateModel : PageModel
{
    private readonly ITestService _tests;
    private readonly AppDbContext _db;

    public CreateModel(ITestService tests, AppDbContext db)
    {
        _tests = tests;
        _db    = db;
    }

    [BindProperty]
    public TestForm Form { get; set; } = new();

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
            var newTest = await _tests.CreateTestAsync(new CreateTestRequest
            {
                Title                  = Form.Title,
                ExamTypeId             = Form.ExamTypeId,
                DurationMinutes        = Form.DurationMinutes,
                PassingPercent         = Form.PassingPercent,
                NegativeMarkingEnabled = Form.NegativeMarkingEnabled
            }, userId);

            TempData["Success"] = "Test created. Add sections and questions below.";
            return Redirect($"/Admin/Content/Tests/Edit?id={newTest.Id}");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return Page();
        }
    }

    private async Task LoadExamTypesAsync(CancellationToken ct)
    {
        var raw = await _db.ExamTypes
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Select(e => new { e.Id, e.Name })
            .ToListAsync(ct);
        ExamTypes = raw.Select(x => (x.Id, x.Name)).ToList();
    }

    public class TestForm
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Exam type is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select an exam type.")]
        [Display(Name = "Exam Type")]
        public int ExamTypeId { get; set; }

        [Range(1, 360)]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; } = 60;

        [Range(0, 100)]
        [Display(Name = "Passing %")]
        public decimal PassingPercent { get; set; } = 35;

        [Display(Name = "Negative Marking")]
        public bool NegativeMarkingEnabled { get; set; } = false;
    }
}
