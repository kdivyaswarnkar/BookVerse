using BookStore.Api.Extensions;
using BookStore.Core.Constants;
using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CouponsController(ICouponService coupons) : ControllerBase
{
    // POST api/coupons/preview   { "code": "WELCOME10" }   (customer: price check, nothing is used up)
    [HttpPost("api/coupons/preview")]
    [Authorize]
    public async Task<ActionResult<CouponPreviewDto>> Preview(PreviewCouponRequest request) =>
        Ok(await coupons.PreviewAsync(User.GetUserId(), request.Code));

    // GET api/admin/coupons
    [HttpGet("api/admin/coupons")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<List<CouponDto>>> List() => Ok(await coupons.ListAsync());

    // POST api/admin/coupons
    [HttpPost("api/admin/coupons")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<CouponDto>> Create(CreateCouponRequest request)
    {
        var dto = await coupons.CreateAsync(request);
        return CreatedAtAction(nameof(List), new { id = dto.Id }, dto);
    }

    // PUT api/admin/coupons/3/active   { "isActive": false }   (switch a coupon on or off)
    [HttpPut("api/admin/coupons/{id:int}/active")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> SetActive(int id, SetCouponActiveRequest request)
    {
        await coupons.SetActiveAsync(id, request.IsActive);
        return NoContent();
    }
}
