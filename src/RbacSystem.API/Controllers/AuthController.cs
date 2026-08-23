using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacSystem.Application.Features.Auth.Login;
using RbacSystem.Application.Features.Auth.Register;

namespace RbacSystem.API.Controllers;

/// <summary>
/// Authentication endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(
    IRegisterUserService registerUserService,
    ILoginService loginService) : ControllerBase
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

    /// <summary>
    /// Authenticates an email and password, returning an access and refresh token pair.
    /// </summary>
    /// <remarks>
    /// An unknown email and an incorrect password return an identical response, so
    /// that this endpoint cannot be used to discover which addresses are registered.
    /// </remarks>
    /// <param name="request">The login payload.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <response code="200">Authenticated; the response carries the token pair.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The email is not registered, or the password is incorrect.</response>
    /// <response code="403">The account is unverified or temporarily locked.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        LoginResult result = await loginService.LoginAsync(
            request,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress,
            cancellationToken);

        return result.Outcome switch
        {
            LoginOutcome.Success => Ok(result.Response),

            LoginOutcome.EmailNotVerified => Problem(
                title: "Login failed",
                detail: "Please verify your email to continue",
                statusCode: StatusCodes.Status403Forbidden),

            LoginOutcome.AccountLocked => Problem(
                title: "Login failed",
                detail: "Account locked due to multiple failed attempts. Try again later.",
                statusCode: StatusCodes.Status403Forbidden),

            // Covers both deactivated and suspended accounts with one message, so the
            // response does not disclose which administrative action was taken.
            LoginOutcome.AccountNotActive => Problem(
                title: "Login failed",
                detail: "This account is not active",
                statusCode: StatusCodes.Status403Forbidden),

            // InvalidCredentials covers both an unregistered address and a bad
            // password: the response must be identical for the two so neither can be
            // told apart. Any future outcome falls back to the same safe answer.
            LoginOutcome.InvalidCredentials or _ => Problem(
                title: "Login failed",
                detail: "Invalid email or password",
                statusCode: StatusCodes.Status401Unauthorized)
        };
    }
}
