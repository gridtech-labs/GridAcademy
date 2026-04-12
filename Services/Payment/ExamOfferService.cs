using GridAcademy.Data;
using GridAcademy.Data.Entities.Payment;
using GridAcademy.DTOs.Payment;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.Payment;

public class ExamOfferService : IExamOfferService
{
    private readonly AppDbContext              _db;
    private readonly ILogger<ExamOfferService> _logger;

    public ExamOfferService(AppDbContext db, ILogger<ExamOfferService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<ValidateOfferResponse> ValidateAsync(ValidateOfferRequest req, CancellationToken ct = default)
    {
        var now  = DateTime.UtcNow;
        var code = req.Code.Trim().ToUpperInvariant();

        var offer = await _db.ExamOffers
            .AsNoTracking()
            .FirstOrDefaultAsync(o =>
                o.Code == code &&
                o.IsActive &&
                (o.ValidFrom == null || o.ValidFrom <= now) &&
                (o.ValidTo   == null || o.ValidTo   >= now) &&
                (o.ExamPageId == null || o.ExamPageId == req.ExamPageId), ct);

        if (offer == null)
            return new ValidateOfferResponse(false, "Invalid or expired offer code.", null, 0, req.OriginalAmount, null);

        if (offer.MaxUses.HasValue && offer.UsedCount >= offer.MaxUses.Value)
            return new ValidateOfferResponse(false, "This offer has reached its maximum usage limit.", null, 0, req.OriginalAmount, null);

        if (req.OriginalAmount < offer.MinOrderAmount)
            return new ValidateOfferResponse(false, $"Minimum order amount of \u20b9{offer.MinOrderAmount} required.", null, 0, req.OriginalAmount, null);

        decimal discount = offer.OfferType switch
        {
            ExamOfferType.PercentageDiscount => Math.Round(req.OriginalAmount * offer.Value / 100, 2),
            ExamOfferType.FixedDiscount      => offer.Value,
            ExamOfferType.FreeAccess         => req.OriginalAmount,
            _                                => 0
        };

        // Apply cap
        if (offer.MaxDiscountAmount.HasValue && discount > offer.MaxDiscountAmount.Value)
            discount = offer.MaxDiscountAmount.Value;

        // Cannot exceed original
        if (discount > req.OriginalAmount) discount = req.OriginalAmount;

        var final = req.OriginalAmount - discount;

        return new ValidateOfferResponse(true, $"Offer '{offer.Title}' applied!", offer.Title, discount, final, offer.Id);
    }

    public async Task<List<ExamOfferDto>> GetActiveOffersForExamAsync(Guid examPageId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.ExamOffers
            .AsNoTracking()
            .Where(o => o.IsActive
                && (o.ExamPageId == null || o.ExamPageId == examPageId)
                && (o.ValidFrom == null || o.ValidFrom <= now)
                && (o.ValidTo   == null || o.ValidTo   >= now)
                && (o.MaxUses   == null || o.UsedCount < o.MaxUses))
            .OrderBy(o => o.Id)
            .Select(o => MapDto(o, null))
            .ToListAsync(ct);
    }

    public async Task<List<ExamOfferDto>> GetAllOffersAsync(CancellationToken ct = default)
    {
        return await _db.ExamOffers
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => MapDto(o, null))
            .ToListAsync(ct);
    }

    public async Task<ExamOfferDto> SaveAsync(int? id, SaveExamOfferRequest req, CancellationToken ct = default)
    {
        var code = req.Code.Trim().ToUpperInvariant();

        // Check uniqueness
        var duplicate = await _db.ExamOffers
            .AnyAsync(o => o.Code == code && (!id.HasValue || o.Id != id.Value), ct);
        if (duplicate) throw new InvalidOperationException($"Offer code '{code}' already exists.");

        ExamOffer entity;
        if (id.HasValue)
        {
            entity = await _db.ExamOffers.FindAsync([id.Value], ct)
                ?? throw new KeyNotFoundException("Offer not found.");
        }
        else
        {
            entity = new ExamOffer();
            _db.ExamOffers.Add(entity);
        }

        entity.Code              = code;
        entity.Title             = req.Title.Trim();
        entity.Description       = req.Description?.Trim();
        entity.OfferType         = req.OfferType;
        entity.Value             = req.Value;
        entity.MinOrderAmount    = req.MinOrderAmount;
        entity.MaxDiscountAmount = req.MaxDiscountAmount;
        entity.ExamPageId        = req.ExamPageId;
        entity.MaxUses           = req.MaxUses;
        entity.IsActive          = req.IsActive;
        entity.ValidFrom         = req.ValidFrom;
        entity.ValidTo           = req.ValidTo;
        entity.UpdatedAt         = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapDto(entity, null);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.ExamOffers.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("Offer not found.");
        _db.ExamOffers.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    private static ExamOfferDto MapDto(ExamOffer o, string? examTitle) => new(
        o.Id, o.Code, o.Title, o.Description, o.OfferType,
        o.Value, o.MinOrderAmount, o.MaxDiscountAmount,
        o.ExamPageId, examTitle,
        o.MaxUses, o.UsedCount, o.IsActive, o.ValidFrom, o.ValidTo);
}
