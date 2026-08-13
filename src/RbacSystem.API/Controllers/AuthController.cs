using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacSystem.Application.Features.Auth.Register;

namespace RbacSystem.API.Controllers;

/// <summary>
/// Authentication endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IRegisterUserService registerUserService) : ControllerBase
{
    /// <summary>
    /// Registers a new user with an email address and password.
    /// </summary>
    /// <remarks>
    /// The account is created unverified and with the default <c>User</c> role. A
    /// registration event is published so that a verification email can be sent
    /// outside this request.
    /// </remarks>
    /// <param name="request">The registration payload.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <response code="201">The account was created.</response>
    /// <response code="400">The payload failed validation, or the email is already registered.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        RegisterResult result = await registerUserService.RegisterAsync(request, cancellationToken);

        return result == RegisterResult.DuplicateEmail
            ? Problem(
                title: "Registration failed",
                detail: "Email is already registered",
                statusCode: StatusCodes.Status400BadRequest)
            : StatusCode(
                StatusCodes.Status201Created,
                new RegisterResponse("Sign Up successful, verify Email."));
    }
}
