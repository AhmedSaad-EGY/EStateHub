namespace EstateHub.Application.Authentication;

public interface IAuthenticationTokenService
{
    Task<AuthenticationTokenPair?> CreateSessionAsync(
        Guid applicationUserId,
        bool rememberMe,
        CancellationToken cancellationToken = default);

    Task<AuthenticationTokenPair?> RotateRefreshTokenAsync(
        string? refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(
        string? refreshToken,
        CancellationToken cancellationToken = default);
}
