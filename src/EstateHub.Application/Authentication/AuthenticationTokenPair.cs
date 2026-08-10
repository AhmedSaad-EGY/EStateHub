namespace EstateHub.Application.Authentication;

public sealed record AuthenticationTokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
