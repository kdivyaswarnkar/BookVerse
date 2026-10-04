using BookStore.Core.DTOs;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

// DEVELOPMENT ONLY. Lets you act as the payment provider, because the fake one has no website.
// Outside Development this endpoint answers 404, as if it did not exist.
[ApiController]
[Route("api/dev/payments")]
[Authorize]
public class PaymentSimulationController(
    IPaymentSimulator simulator,
    IPaymentService payments,
    IWebHostEnvironment env) : ControllerBase
{
    // POST api/dev/payments/simulate
    // { "providerPaymentId": "fake_pay_...", "success": true, "badSignature": false }
    [HttpPost("simulate")]
    public async Task<IActionResult> Simulate(SimulatePaymentRequest request)
    {
        if (!env.IsDevelopment()) return NotFound();

        var (body, signature) = simulator.CreateWebhook(request.ProviderPaymentId, request.Success);
        if (request.BadSignature) signature = "0000";

        // goes through exactly the same code as the real webhook, including the signature check
        if (!await payments.HandleWebhookAsync(body, signature))
            throw new AppException("Webhook rejected: invalid signature.", 400);

        return Ok(new MessageResponse(request.Success
            ? "Simulated: provider reported SUCCESS."
            : "Simulated: provider reported FAILURE."));
    }
}