using BookStore.Core.Constants;

namespace BookStore.Tests.Unit;

public class OrderStatusesTests
{
    [Theory]
    [InlineData("Pending", "Cancelled", true)]
    [InlineData("Paid", "Shipped", true)]
    [InlineData("Shipped", "Delivered", true)]
    [InlineData("Pending", "Paid", false)]        // only the payment webhook may do this
    [InlineData("Pending", "Shipped", false)]     // cannot ship an unpaid order
    [InlineData("Paid", "Cancelled", false)]      // a refund is needed first (not built yet)
    [InlineData("Paid", "Delivered", false)]      // cannot skip a step
    [InlineData("Delivered", "Cancelled", false)]
    [InlineData("Cancelled", "Pending", false)]   // a cancelled order stays cancelled
    [InlineData("Shipped", "Paid", false)]        // no going backwards
    public void CanMove_FollowsTheStatusFlow(string from, string to, bool expected) =>
        Assert.Equal(expected, OrderStatuses.CanMove(from, to));

    [Fact]
    public void CanMove_UnknownStatus_IsRefused() =>
        Assert.False(OrderStatuses.CanMove("Banana", "Cancelled"));

    [Fact]
    public void FinalStatuses_HaveNoWayOut()
    {
        foreach (var from in new[] { OrderStatuses.Delivered, OrderStatuses.Cancelled })
            foreach (var to in OrderStatuses.All)
                Assert.False(OrderStatuses.CanMove(from, to), $"{from} -> {to} must not be allowed");
    }
}
