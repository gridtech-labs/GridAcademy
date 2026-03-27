using GridAcademy.Common;
using GridAcademy.DTOs.VideoLearning;

namespace GridAcademy.Services.VideoLearning;

public interface ICouponService
{
    Task<PagedResult<CouponDto>>  GetCouponsAsync(CouponListRequest request);
    Task<CouponDto>               GetByIdAsync(int id);
    Task<CouponDto>               CreateAsync(CreateCouponRequest request);
    Task<CouponDto>               UpdateAsync(int id, UpdateCouponRequest request);
    Task                          DeleteAsync(int id);
    Task<CouponValidationResult>  ValidateAsync(string code, Guid programId, int pricingPlanId);
}
