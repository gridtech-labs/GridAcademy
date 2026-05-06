using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Provider.Tests;

[Authorize(Roles = "Provider")]
public class CreateModel : PageModel
{
    private readonly ITestService _tests;

    public CreateModel(ITestService tests) => _tests = tests;

    [BindProperty]
    public TestForm Form { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken ct = default) => Page();

    public async Task<IActionResult> OnPostAsync(CancellationToken ct = default)
    {

        if (!ModelState.IsValid) return Page();

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var newTest = await _tests.CreateTestAsync(new CreateTestRequest
            {
                Title                  = Form.Title,
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

    public class TestForm
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

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
