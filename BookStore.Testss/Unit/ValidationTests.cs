using System.ComponentModel.DataAnnotations;
using BookStore.Core.DTOs;

namespace BookStore.Tests.Unit;

// Checks the validation attributes on request DTOs (the same check [ApiController] runs before the action).
public class ValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    [InlineData(-3, false)]
    public void ReviewRating_MustBeOneToFive(int rating, bool valid) =>
        Assert.Equal(valid, Validate(new SaveReviewRequest { Rating = rating }).Count == 0);

    [Fact]
    public void ReviewComment_CannotBeLongerThan1000()
    {
        var tooLong = new SaveReviewRequest { Rating = 4, Comment = new string('a', 1001) };
        Assert.NotEmpty(Validate(tooLong));
    }

    private static CreateCouponRequest ValidCoupon() => new()
    {
        Code = "DIWALI15", DiscountType = "Percent", DiscountValue = 15, MinOrderAmount = 300, MaxUses = 50,
        ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddDays(30)
    };

    [Fact]
    public void ValidCoupon_Passes() => Assert.Empty(Validate(ValidCoupon()));

    [Theory]
    [InlineData("ab")]            // too short
    [InlineData("BAD CODE!")]     // space and symbol
    [InlineData("")]
    public void CouponCode_MustMatchThePattern(string code)
    {
        var request = ValidCoupon();
        request.Code = code;
        Assert.NotEmpty(Validate(request));
    }

    [Fact]
    public void CouponType_MustBePercentOrFlat()
    {
        var request = ValidCoupon();
        request.DiscountType = "Bogus";
        Assert.NotEmpty(Validate(request));
    }

    [Fact]
    public void CouponDiscountValue_MustBePositive()
    {
        var request = ValidCoupon();
        request.DiscountValue = 0;
        Assert.NotEmpty(Validate(request));
    }

    [Fact]
    public void OrderRequest_NeedsAnAddress() =>
        Assert.NotEmpty(Validate(new PlaceOrderRequest { AddressId = 0 }));
}
