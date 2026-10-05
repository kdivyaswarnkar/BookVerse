using BookStore.Core.Constants;
using BookStore.Core.Entities;

namespace BookStore.Core.Rules;

// The coupon rules in plain C#, used for the PREVIEW ("what would I pay?").
// The real, final check happens inside sp_PlaceOrder, because only SQL can claim the coupon safely.
// Keep the two in step: same rules, same rounding.
public static class CouponRules
{
    public static (bool Ok, string Message, decimal Discount) Evaluate(Coupon? c, decimal subtotal, DateTime nowUtc)
    {
        if (c is null) return (false, "Coupon code not found", 0);
        if (!c.IsActive || nowUtc < c.ValidFrom || nowUtc > c.ValidTo) return (false, "Coupon is not active or has expired", 0);
        if (c.MaxUses is not null && c.UsedCount >= c.MaxUses) return (false, "Coupon usage limit has been reached", 0);
        if (subtotal < c.MinOrderAmount) return (false, $"Order total is below the minimum ({c.MinOrderAmount:0.00}) for this coupon", 0);

        var discount = c.DiscountType == CouponTypes.Percent
            ? Math.Round(subtotal * c.DiscountValue / 100m, 2, MidpointRounding.AwayFromZero)
            : Math.Min(c.DiscountValue, subtotal);

        return (true, "Coupon applied", discount);
    }
}
