namespace RbacSystem.Application.Features.Auth.Register;

/// <summary>
/// Outcome of a registration attempt that passed input validation.
/// </summary>
public enum RegisterResult
{
    /// <summary>The user was created.</summary>
    Success = 0,

    /// <summary>The email address is already registered.</summary>
    DuplicateEmail = 1
}
