using GridAcademy.Data;
using GridAcademy.Data.Entities.Exam;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Student;

[Authorize(Roles = "User")]
public class AvailableTestsModel : PageModel
{
    private readonly AppDbContext _db;

    public AvailableTestsModel(AppDbContext db)
    {
        _db = db;
    }

    public List<ExamSummary> Exams { get; set; } = [];

    public async Task OnGetAsync()
    {
        Exams = await _db.ExamPages
            .Where(e => e.Status == ExamPageStatus.Published && e.IsActive)
            .OrderByDescending(e => e.IsFeatured)
            .ThenBy(e => e.SortOrder)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => new ExamSummary
            {
                Id               = e.Id,
                Title            = e.Title,
                Slug             = e.Slug,
                ShortDescription = e.ShortDescription,
                ThumbnailUrl     = e.ThumbnailUrl,
                ExamTypeName     = e.ExamType != null ? e.ExamType.Name : null,
                IsFeatured       = e.IsFeatured,
                PriceInr         = e.PriceInr,
                TestCount        = e.Tests.Count,
                FreeTestCount    = e.Tests.Count(t => t.IsFree),
            })
            .ToListAsync();
    }

    public class ExamSummary
    {
        public Guid    Id               { get; set; }
        public string  Title            { get; set; } = "";
        public string  Slug             { get; set; } = "";
        public string? ShortDescription { get; set; }
        public string? ThumbnailUrl     { get; set; }
        public string? ExamTypeName     { get; set; }
        public bool    IsFeatured       { get; set; }
        public decimal PriceInr         { get; set; }
        public int     TestCount        { get; set; }
        public int     FreeTestCount    { get; set; }
    }
}
