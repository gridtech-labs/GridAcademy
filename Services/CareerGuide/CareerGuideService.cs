using GridAcademy.Data;
using GridAcademy.Data.Entities.CareerGuide;
using GridAcademy.DTOs.CareerGuide;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.CareerGuide;

public class CareerGuideService(AppDbContext db) : ICareerGuideService
{
    public async Task<List<CareerQuizQuestionDto>> GetActiveQuestionsAsync(CancellationToken ct = default) =>
        await db.CareerQuizQuestions
            .Where(q => q.IsActive)
            .OrderBy(q => q.SortOrder)
            .Select(q => MapToDto(q))
            .ToListAsync(ct);

    public async Task<List<CareerQuizQuestionDto>> GetAllQuestionsAsync(CancellationToken ct = default) =>
        await db.CareerQuizQuestions
            .OrderBy(q => q.SortOrder)
            .Select(q => MapToDto(q))
            .ToListAsync(ct);

    public async Task<CareerQuizQuestionDto> CreateQuestionAsync(CreateQuizQuestionRequest req, CancellationToken ct = default)
    {
        var q = new CareerQuizQuestion
        {
            QuestionText = req.QuestionText.Trim(),
            SortOrder    = req.SortOrder,
            IsActive     = req.IsActive,
            CreatedAt    = DateTime.UtcNow,
            Options      = req.Options.Select((o, i) => new CareerQuizOption
            {
                OptionText     = o.OptionText.Trim(),
                CareerCategory = o.CareerCategory.Trim().ToLower(),
                SortOrder      = o.SortOrder > 0 ? o.SortOrder : i,
            }).ToList(),
        };
        db.CareerQuizQuestions.Add(q);
        await db.SaveChangesAsync(ct);
        return MapToDto(q);
    }

    public async Task<bool> UpdateQuestionAsync(int id, UpdateQuizQuestionRequest req, CancellationToken ct = default)
    {
        var q = await db.CareerQuizQuestions
            .Include(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null) return false;

        q.QuestionText = req.QuestionText.Trim();
        q.SortOrder    = req.SortOrder;
        q.IsActive     = req.IsActive;

        // Replace options
        db.CareerQuizOptions.RemoveRange(q.Options);
        q.Options = req.Options.Select((o, i) => new CareerQuizOption
        {
            QuestionId     = id,
            OptionText     = o.OptionText.Trim(),
            CareerCategory = o.CareerCategory.Trim().ToLower(),
            SortOrder      = o.SortOrder > 0 ? o.SortOrder : i,
        }).ToList();

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteQuestionAsync(int id, CancellationToken ct = default)
    {
        var q = await db.CareerQuizQuestions.FindAsync([id], ct);
        if (q is null) return false;
        db.CareerQuizQuestions.Remove(q);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        var q = await db.CareerQuizQuestions.FindAsync([id], ct);
        if (q is null) return false;
        q.IsActive = !q.IsActive;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static CareerQuizQuestionDto MapToDto(CareerQuizQuestion q) => new(
        q.Id,
        q.QuestionText,
        q.SortOrder,
        q.IsActive,
        q.Options
            .OrderBy(o => o.SortOrder)
            .Select(o => new CareerQuizOptionDto(o.Id, o.OptionText, o.CareerCategory, o.SortOrder))
            .ToList());
}
