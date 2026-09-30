using RbacSystem.Domain.Enums;

namespace RbacSystem.Application.Interfaces.Repositories;

/// <summary>
/// Minimal current user state required to validate an already-issued access token.
/// </summary>
/// <param name="TokenVersion">Current account-wide token version.</param>
/// <param name="Status">Current account status.</param>
public sealed record UserSecurityState(int TokenVersion, UserStatus Status);
