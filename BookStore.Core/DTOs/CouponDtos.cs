using System.ComponentModel.DataAnnotations;
using BookStore.Core.Constants;

namespace BookStore.Core.DTOs;

public class PreviewCouponRequest
{
    [Required, MaxLength(30)]
    public string Code { get; set; } = "";
}

public class CouponPreviewDto
{
    public string Code { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string Message { get; set; } = "";
}

public class CreateCouponRequest
{
    [Required, RegularExpression("^[A-Za-z0-9_-]{3,30}$", ErrorMessage = "Code must be 3 to 30 letters, digits, _ or -.")]
    public string Code { get; set; } = "";

    [Required, AllowedValues(CouponTypes.Percent, CouponTypes.Flat)]
    public string DiscountType { get; set; } = CouponTypes.Percent;

    [Range(0.01, 100000)]
    public decimal DiscountValue { get; set; }

    [Range(0, 1000000)]
    public decimal MinOrderAmount { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxUses { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
}

public class SetCouponActiveRequest
{
    public bool IsActive { get; set; }
}

public class CouponDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string DiscountType { get; set; } = "";
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; }
}
