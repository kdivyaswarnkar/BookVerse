using BookStore.Api.Extensions;
using BookStore.Core.Constants;
using BookStore.Core.DTOs;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Policy = Policies.AdminOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminOrdersController(IOrderAdminService orders, IOutputCacheStore outputCache) : ControllerBase
{
    // GET api/admin/orders?status=Paid&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminOrderListItemDto>>> List(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(status) && !OrderStatuses.All.Contains(status))
            throw new AppException($"Status must be one of: {string.Join(", ", OrderStatuses.All)}.", 400);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await orders.ListAsync(status, page, pageSize));
    }

    // GET api/admin/orders/12
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminOrderDetailDto>> Get(int id)
    {
        var order = await orders.GetAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    // PUT api/admin/orders/12/status      { "status": "Shipped" }
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        await orders.UpdateStatusAsync(User.GetUserId(), id, request.Status);

        // a cancelled order gives its stock back, so cached book pages are out of date
        if (request.Status == OrderStatuses.Cancelled)
            await outputCache.EvictByTagAsync("books", ct);

        return NoContent();
    }
}