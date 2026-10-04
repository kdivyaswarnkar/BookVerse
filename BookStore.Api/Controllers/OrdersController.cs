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
    // POST api/orders      { "addressId": 3 }   (the order is made from the current cart)
    [HttpPost]
    public async Task<ActionResult<PlaceOrderResponse>> Place(PlaceOrderRequest request, CancellationToken ct)
    {
        var result = await orders.PlaceOrderAsync(User.GetUserId(), request.AddressId);

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
}