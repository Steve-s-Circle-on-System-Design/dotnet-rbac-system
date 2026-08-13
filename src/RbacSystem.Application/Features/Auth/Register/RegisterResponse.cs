namespace RbacSystem.Application.Features.Auth.Register;

/// <summary>
/// Successful registration response.
/// </summary>
/// <param name="Message">Human-readable confirmation for the caller.</param>
public sealed record RegisterResponse(string Message);
