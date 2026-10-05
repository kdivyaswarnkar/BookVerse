using BookStore.Core.Entities;
using BookStore.Core.Rules;

namespace BookStore.Tests.Unit;

public class CouponRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    private static Coupon Make(string type = "Percent", decimal value = 10, decimal min = 0, int? maxUses = null,
        int used = 0, bool active = true, int fromDays = -1, int toDays = 1) => new()
    {
        Code = "X", DiscountType = type, DiscountValue = value, MinOrderAmount = min, MaxUses = maxUses, UsedCount = used,
        IsActive = active, ValidFrom = Now.AddDays(fromDays), ValidTo = Now.AddDays(toDays)
    };

    [Fact]
    public void Percent_IsCalculatedOnTheSubtotal()
    {
        var (ok, _, discount) = CouponRules.Evaluate(Make("Percent", 10), 200m, Now);
        Assert.True(ok);
        Assert.Equal(20m, discount);
    }

    [Fact]
    public void Percent_RoundsToTwoDecimals_AwayFromZero()
    {
        // 15% of 33.33 = 4.9995 -> 5.00
        var (_, _, discount) = CouponRules.Evaluate(Make("Percent", 15), 33.33m, Now);
        Assert.Equal(5.00m, discount);
    }

    [Fact]
    public void Flat_IsTheFixedAmount()
    {
        var (_, _, discount) = CouponRules.Evaluate(Make("Flat", 50), 600m, Now);
        Assert.Equal(50m, discount);
    }

    [Fact]
    public void Flat_NeverMakesTheTotalNegative()
    {
        var (ok, _, discount) = CouponRules.Evaluate(Make("Flat", 50), 30m, Now);
        Assert.True(ok);
        Assert.Equal(30m, discount);
    }

    [Fact]
    public void UnknownCoupon_IsRefused()
    {
        var (ok, message, _) = CouponRules.Evaluate(null, 100m, Now);
        Assert.False(ok);
        Assert.Contains("not found", message);
    }

    [Theory]
    [InlineData(false, -1, 1)]     // switched off
    [InlineData(true, -10, -5)]    // expired
    [InlineData(true, 1, 10)]      // not started yet
    public void InactiveOrOutsideDates_IsRefused(bool active, int fromDays, int toDays)
    {
        var (ok, message, discount) = CouponRules.Evaluate(Make(active: active, fromDays: fromDays, toDays: toDays), 100m, Now);
        Assert.False(ok);
        Assert.Contains("expired", message);
        Assert.Equal(0m, discount);
    }

    [Fact]
    public void UsageLimitReached_IsRefused()
    {
        var (ok, message, _) = CouponRules.Evaluate(Make(maxUses: 5, used: 5), 100m, Now);
        Assert.False(ok);
        Assert.Contains("limit", message);
    }

    [Fact]
    public void LastRemainingUse_StillWorks()
    {
        var (ok, _, _) = CouponRules.Evaluate(Make(maxUses: 5, used: 4), 100m, Now);
        Assert.True(ok);
    }

    [Fact]
    public void BelowMinimumOrder_IsRefused_AtTheMinimumItWorks()
    {
        Assert.False(CouponRules.Evaluate(Make(min: 500), 499.99m, Now).Ok);
        Assert.True(CouponRules.Evaluate(Make(min: 500), 500m, Now).Ok);
    }
}
