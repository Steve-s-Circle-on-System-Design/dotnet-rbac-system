namespace RbacSystem.Application.Features.Auth.Register;

/// <summary>
/// Registers new users from an email address and password.
/// </summary>
public interface IRegisterUserService
{
    /// <summary>
    /// Creates an unverified user with the default <c>User</c> role.
    /// </summary>
    /// <param name="request">The validated registration request.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The outcome of the attempt.</returns>
    Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
