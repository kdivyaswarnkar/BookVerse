using BookStore.Core.Constants;
using Microsoft.AspNetCore.RateLimiting;
using BookStore.Api.Extensions;
using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]   // private data
public class OrdersController(IOrderService orders, IOutputCacheStore outputCache) : ControllerBase
{
    // POST api/orders      { "addressId": 3, "couponCode": "WELCOME10" }   (couponCode is optional)   (the order is made from the current cart)
    [EnableRateLimiting(RateLimitPolicies.Checkout)]
    [HttpPost]
    public async Task<ActionResult<PlaceOrderResponse>> Place(PlaceOrderRequest request, CancellationToken ct)
    {
        var result = await orders.PlaceOrderAsync(User.GetUserId(), request.AddressId, request.CouponCode);

        // stock has changed, so cached book pages are out of date
        await outputCache.EvictByTagAsync("books", ct);

        return CreatedAtAction(nameof(Get), new { id = result.OrderId }, result);
    }

    // GET api/orders
    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryDto>>> List() =>
        Ok(await orders.ListAsync(User.GetUserId()));

    // GET api/orders/12
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDetailDto>> Get(int id)
    {
        var order = await orders.GetAsync(User.GetUserId(), id);
        return order is null ? NotFound() : Ok(order);
    }

    // POST api/orders/12/cancel     (only while the order is still Pending)
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await orders.CancelAsync(User.GetUserId(), id);

        // the stock came back
        await outputCache.EvictByTagAsync("books", ct);

        return NoContent();
    }
}
