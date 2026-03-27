using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.VideoLearning;

public class CouponService(AppDbContext db) : ICouponService
{
    private static CouponDto Map(VlCoupon c) => new(
        c.Id, c.Code, c.Description, c.DiscountType, c.DiscountValue,
        c.MaxDiscountInr, c.MaxDiscountUsd, c.ValidFrom, c.ValidTo,
        c.UsageLimit, c.UsedCount, c.ProgramId, c.Program?.Title, c.IsActive);

    public async Task<PagedResult<CouponDto>> GetCouponsAsync(CouponListRequest request)
    {
        var q = db.VlCoupons.Include(c => c.Program).AsQueryable();
        if (request.ProgramId.HasValue) q = q.Where(c => c.ProgramId == request.ProgramId.Value);
        if (request.IsActive.HasValue)  q = q.Where(c => c.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
            q = q.Where(c => c.Code.Contains(request.Search) || (c.Description != null && c.Description.Contains(request.Search)));

        var total = await q.CountAsync();
        var items = (await q.OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync())
            .Select(Map).ToList();
        return new PagedResult<CouponDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<CouponDto> GetByIdAsync(int id)
    {
        var c = await db.VlCoupons.Include(x => x.Program).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Coupon {id} not found.");
        return Map(c);
    }

    public async Task<CouponDto> CreateAsync(CreateCouponRequest request)
    {
        var code = request.Code.ToUpperInvariant().Trim();
        if (await db.VlCoupons.AnyAsync(c => c.Code == code))
            throw new InvalidOperationException($"Coupon code '{code}' already exists.");
        var entity = new VlCoupon {
            Code = code, Description = request.Description, DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue, MaxDiscountInr = request.MaxDiscountInr,
            MaxDiscountUsd = request.MaxDiscountUsd, ValidFrom = request.ValidFrom, ValidTo = request.ValidTo,
            UsageLimit = request.UsageLimit, ProgramId = request.ProgramId, IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VlCoupons.Add(entity);
        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<CouponDto> UpdateAsync(int id, UpdateCouponRequest request)
    {
        var entity = await db.VlCoupons.Include(x => x.Program).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Coupon {id} not found.");
        entity.Description = request.Description; entity.DiscountType = request.DiscountType;
        entity.DiscountValue = request.DiscountValue; entity.MaxDiscountInr = request.MaxDiscountInr;
        entity.MaxDiscountUsd = request.MaxDiscountUsd; entity.ValidFrom = request.ValidFrom;
        entity.ValidTo = request.ValidTo; entity.UsageLimit = request.UsageLimit;
        entity.ProgramId = request.ProgramId; entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.VlCoupons.FindAsync(id)
            ?? throw new KeyNotFoundException($"Coupon {id} not found.");
        db.VlCoupons.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<CouponValidationResult> ValidateAsync(string code, Guid programId, int pricingPlanId)
    {
        var upper = code.ToUpperInvariant().Trim();
        var coupon = await db.VlCoupons.FirstOrDefaultAsync(c => c.Code == upper && c.IsActive);
        if (coupon == null)
            return new CouponValidationResult(false, "Invalid or inactive coupon code.", 0, 0);
        if (coupon.ProgramId.HasValue && coupon.ProgramId != programId)
            return new CouponValidationResult(false, "This coupon is not valid for this program.", 0, 0);
        var now = DateTime.UtcNow;
        if (coupon.ValidFrom.HasValue && now < coupon.ValidFrom.Value)
            return new CouponValidationResult(false, "This coupon is not yet active.", 0, 0);
        if (coupon.ValidTo.HasValue && now > coupon.ValidTo.Value)
            return new CouponValidationResult(false, "This coupon has expired.", 0, 0);
        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            return new CouponValidationResult(false, "This coupon has reached its usage limit.", 0, 0);

        var plan = await db.VlProgramPricingPlans.FindAsync(pricingPlanId);
        if (plan == null) return new CouponValidationResult(false, "Pricing plan not found.", 0, 0);

        decimal discInr, discUsd;
        if (coupon.DiscountType == DiscountType.Percentage)
        {
            discInr = Math.Round(plan.PriceInr * coupon.DiscountValue / 100, 2);
            discUsd = Math.Round(plan.PriceUsd * coupon.DiscountValue / 100, 2);
            if (coupon.MaxDiscountInr.HasValue) discInr = Math.Min(discInr, coupon.MaxDiscountInr.Value);
            if (coupon.MaxDiscountUsd.HasValue) discUsd = Math.Min(discUsd, coupon.MaxDiscountUsd.Value);
        }
        else
        {
            discInr = Math.Min(coupon.DiscountValue, plan.PriceInr);
            discUsd = Math.Min(coupon.DiscountValue, plan.PriceUsd);
        }
        return new CouponValidationResult(true, null, discInr, discUsd);
    }
}
