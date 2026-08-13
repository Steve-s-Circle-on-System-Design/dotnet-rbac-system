namespace RbacSystem.Application.Features.Auth.Register;

/// <summary>
/// Raised once a new user has been persisted, so that email verification can be
/// delivered outside the registration request.
/// </summary>
/// <param name="UserId">Identifier of the newly created user.</param>
/// <param name="Email">The user's normalized email address.</param>
/// <param name="OccurredAtUtc">When the registration completed, in UTC.</param>
public sealed record UserRegisteredEvent(string UserId, string Email, DateTime OccurredAtUtc);
