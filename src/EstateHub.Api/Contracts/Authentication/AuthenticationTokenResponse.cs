using EstateHub.Application.Authentication;

namespace EstateHub.Api.Contracts.Authentication;

public sealed record AuthenticationTokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public static AuthenticationTokenResponse From(
        AuthenticationTokenPair tokenPair)
    {
        return new AuthenticationTokenResponse(
            tokenPair.AccessToken,
            tokenPair.AccessTokenExpiresAt,
            tokenPair.RefreshToken,
            tokenPair.RefreshTokenExpiresAt);
    }
}
