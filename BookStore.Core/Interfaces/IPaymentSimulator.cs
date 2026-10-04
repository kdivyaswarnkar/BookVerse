namespace BookStore.Core.Interfaces;

// Development helper: builds a correctly signed webhook message, like the provider would send
public interface IPaymentSimulator
{
    (string Body, string Signature) CreateWebhook(string providerPaymentId, bool success);
}