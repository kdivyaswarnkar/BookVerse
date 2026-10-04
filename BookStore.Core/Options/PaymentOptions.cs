namespace BookStore.Core.Options;

// Bound to the "Payments" section. The secret lives in secrets.json, never in appsettings.json.
public class PaymentOptions
{
    // Shared secret used to sign and verify webhook messages from the payment provider
    public string WebhookSecret { get; set; } = "";
}