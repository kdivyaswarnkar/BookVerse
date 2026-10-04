using System.Text;
using BookStore.Api.Extensions;
using BookStore.Core.DTOs;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/payments")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PaymentsController(IPaymentService payments) : ControllerBase
{
    // POST api/payments/create     { "orderId": 12 }
    [Authorize]
    [HttpPost("create")]
    public async Task<ActionResult<CreatePaymentResponse>> Create(CreatePaymentRequest request) =>
        Ok(await payments.CreatePaymentAsync(User.GetUserId(), request.OrderId));

    // GET api/payments/order/12
    [Authorize]
    [HttpGet("order/{orderId:int}")]
    public async Task<ActionResult<PaymentStatusDto>> Status(int orderId)
    {
        var status = await payments.GetStatusAsync(User.GetUserId(), orderId);
        return status is null ? NotFound() : Ok(status);
    }

    // POST api/payments/webhook
    // Called by the payment provider, NOT by the browser, so there is no login token.
    // Trust comes from the signature in the X-Signature header instead.
    [AllowAnonymous]
    [HttpPost("webhook")]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> Webhook()
    {
        // the signature is calculated over the exact raw text, so read it as text
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Signature"].FirstOrDefault();

        if (!await payments.HandleWebhookAsync(body, signature))
            throw new AppException("Invalid signature.", 400);

        return Ok();
    }
}