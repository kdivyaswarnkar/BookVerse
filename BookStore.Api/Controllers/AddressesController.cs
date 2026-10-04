using BookStore.Api.Extensions;
using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/addresses")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]   // private data
public class AddressesController(IAddressService addresses) : ControllerBase
{
    // GET api/addresses
    [HttpGet]
    public async Task<ActionResult<List<AddressDto>>> List() =>
        Ok(await addresses.ListAsync(User.GetUserId()));

    // POST api/addresses
    [HttpPost]
    public async Task<ActionResult<AddressDto>> Add(AddressRequest request) =>
        StatusCode(201, await addresses.AddAsync(User.GetUserId(), request));

    // PUT api/addresses/5   (returns the NEW address, with a new id)
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AddressDto>> Replace(int id, AddressRequest request) =>
        Ok(await addresses.ReplaceAsync(User.GetUserId(), id, request));

    // DELETE api/addresses/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await addresses.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }
}