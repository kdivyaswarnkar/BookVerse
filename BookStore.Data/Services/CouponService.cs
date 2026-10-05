using BookStore.Core.Constants;
using BookStore.Core.DTOs;
using BookStore.Core.Entities;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using BookStore.Core.Rules;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data.Services;

public class CouponService(AppDbContext db) : ICouponService
{
    public async Task<CouponPreviewDto> PreviewAsync(int userId, string code)
    {
        code = code.Trim();

        // cart subtotal for THIS user (id comes from the token)
        var subtotal = await db.CartItems
            .Where(c => c.UserId == userId)
            .SumAsync(c => (decimal?)(c.Book.Price * c.Quantity)) ?? 0m;

        if (subtotal == 0)
            throw new AppException("Your cart is empty.", 400);

        var coupon = await db.Set<Coupon>().AsNoTracking().FirstOrDefaultAsync(c => c.Code == code);
        var (ok, message, discount) = CouponRules.Evaluate(coupon, subtotal, DateTime.UtcNow);

        if (!ok) throw new AppException(message, 400);

        return new CouponPreviewDto
        {
            Code = coupon!.Code, Subtotal = subtotal, Discount = discount,
            Total = subtotal - discount, Message = message
        };
    }

    public Task<List<CouponDto>> ListAsync() =>
        db.Set<Coupon>().AsNoTracking().OrderByDescending(c => c.Id).Select(ToDto).ToListAsync();

    public async Task<CouponDto> CreateAsync(CreateCouponRequest r)
    {
        if (r.DiscountType == CouponTypes.Percent && r.DiscountValue > 100)
            throw new AppException("A percent discount cannot be more than 100.", 400);
        if (r.ValidTo <= r.ValidFrom)
            throw new AppException("ValidTo must be after ValidFrom.", 400);

        var coupon = new Coupon
        {
            Code = r.Code.Trim().ToUpperInvariant(),
            DiscountType = r.DiscountType,
            DiscountValue = r.DiscountValue,
            MinOrderAmount = r.MinOrderAmount,
            MaxUses = r.MaxUses,
            ValidFrom = r.ValidFrom.ToUniversalTime(),
            ValidTo = r.ValidTo.ToUniversalTime(),
            IsActive = true
        };
        db.Add(coupon);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new AppException("A coupon with this code already exists.", 409);
        }

        return ToDto.Compile()(coupon);
    }

    public async Task SetActiveAsync(int couponId, bool isActive)
    {
        var rows = await db.Set<Coupon>().Where(c => c.Id == couponId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, isActive));
        if (rows == 0) throw new AppException("Coupon not found.", 404);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Coupon, CouponDto>> ToDto = c => new CouponDto
    {
        Id = c.Id, Code = c.Code, DiscountType = c.DiscountType, DiscountValue = c.DiscountValue,
        MinOrderAmount = c.MinOrderAmount, MaxUses = c.MaxUses, UsedCount = c.UsedCount,
        ValidFrom = c.ValidFrom, ValidTo = c.ValidTo, IsActive = c.IsActive
    };
}
