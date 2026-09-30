using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacSystem.API.Authorization;

namespace RbacSystem.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AppPolicies.RequireAdmin)]
public class AdminController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Admin access granted" });
    }
}
