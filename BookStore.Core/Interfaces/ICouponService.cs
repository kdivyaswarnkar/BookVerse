using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface ICouponService
{
    // customer: "what would my current cart cost with this code?" (nothing is saved or used up)
    Task<CouponPreviewDto> PreviewAsync(int userId, string code);

    // admin
    Task<List<CouponDto>> ListAsync();
    Task<CouponDto> CreateAsync(CreateCouponRequest request);
    Task SetActiveAsync(int couponId, bool isActive);
}
