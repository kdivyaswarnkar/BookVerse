namespace BookStore.Core.Entities;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string DiscountType { get; set; } = "Percent";   // Percent or Flat
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int? MaxUses { get; set; }                        // null = unlimited
    public int UsedCount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
