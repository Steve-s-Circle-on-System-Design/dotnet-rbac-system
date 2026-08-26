using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacSystem.Domain.Common;

namespace RbacSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPolicies.RequireAdmin)]
public class AdminController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Admin access granted" });
    }
}
