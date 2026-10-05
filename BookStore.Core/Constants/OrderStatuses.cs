namespace BookStore.Core.Constants;

public static class OrderStatuses
{
    public const string Pending = "Pending";       // placed, waiting for payment
    public const string Paid = "Paid";             // set only by the payment webhook
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Pending, Paid, Shipped, Delivered, Cancelled];

    // The allowed moves. Everything not listed here is refused.
    //   Pending   -> Cancelled   (unpaid orders can be cancelled)
    //   Paid      -> Shipped     (a paid order would need a REFUND to be cancelled, not built yet)
    //   Shipped   -> Delivered
    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        [Pending] = [Cancelled],
        [Paid] = [Shipped],
        [Shipped] = [Delivered],
        [Delivered] = [],
        [Cancelled] = []
    };

    public static bool CanMove(string from, string to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);
}