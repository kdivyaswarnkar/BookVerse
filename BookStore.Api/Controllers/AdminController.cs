using BookStore.Core.Constants;
using BookStore.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

// Every endpoint in this controller needs the Admin role
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminController : ControllerBase
{
    // GET api/admin/ping   (temporary test endpoint; BE-07 adds the real admin APIs here)
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new MessageResponse("Hello admin, you have access."));
}