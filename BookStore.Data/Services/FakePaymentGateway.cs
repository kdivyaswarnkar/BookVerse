using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using BookStore.Core.Options;
using Microsoft.Extensions.Options;

namespace BookStore.Data.Services;

// A pretend payment provider for development. It behaves like a real one:
// it hands out payment ids, and later "calls our webhook" with a signed message.
public class FakePaymentGateway(IOptions<PaymentOptions> options) : IPaymentGateway, IPaymentSimulator
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string Name => "Fake";

    public Task<string> CreatePaymentAsync(decimal amount, int orderId) =>
        Task.FromResult($"fake_pay_{Guid.NewGuid():N}");

    public PaymentWebhookEvent? ReadWebhook(string body, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature)) return null;

        // timing-safe comparison: it takes the same time however many characters match
        var expected = Encoding.UTF8.GetBytes(Sign(body));
        var received = Encoding.UTF8.GetBytes(signature.Trim());
        if (!CryptographicOperations.FixedTimeEquals(expected, received)) return null;

        try
        {
            return JsonSerializer.Deserialize<PaymentWebhookEvent>(body, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public (string Body, string Signature) CreateWebhook(string providerPaymentId, bool success)
    {
        var body = JsonSerializer.Serialize(
            new PaymentWebhookEvent(providerPaymentId, success ? "succeeded" : "failed"), Json);
        return (body, Sign(body));
    }

    // HMAC-SHA256 of the message body, using the shared secret
    private string Sign(string body) =>
        Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(options.Value.WebhookSecret),
            Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
}