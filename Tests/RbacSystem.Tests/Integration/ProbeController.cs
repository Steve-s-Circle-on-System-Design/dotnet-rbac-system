using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RbacSystem.Tests.Integration;

/// <summary>
/// Protected endpoints that exist only inside the test assembly, so the real
/// authentication pipeline can be exercised without shipping a probe endpoint in
/// the API.
/// </summary>
/// <remarks>
/// Registered through <c>AddApplicationPart</c> by <see cref="AuthApiFactory"/>.
/// </remarks>
[ApiController]
[Route("test-probe")]
public sealed class ProbeController : ControllerBase
{
    /// <summary>Requires only a valid token, whatever the role.</summary>
    [HttpGet("authenticated")]
    [Authorize]
    public IActionResult Authenticated()
    {
        return Ok(new
        {
            subject = User.Identity?.Name,
            role = User.FindFirst("role")?.Value,
            isInUserRole = User.IsInRole("user"),
            isInAdminRole = User.IsInRole("admin")
        });
    }

    /// <summary>Requires the <c>user</c> role specifically.</summary>
    [HttpGet("user-only")]
    [Authorize(Roles = "user")]
    public IActionResult UserOnly()
    {
        return Ok(new { ok = true });
    }

    /// <summary>Requires the <c>admin</c> role, which the test account does not hold.</summary>
    [HttpGet("admin-only")]
    [Authorize(Roles = "admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new { ok = true });
    }
}
