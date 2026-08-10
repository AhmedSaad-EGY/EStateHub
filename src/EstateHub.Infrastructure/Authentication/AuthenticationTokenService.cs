using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EstateHub.Application.Authentication;
using EstateHub.Infrastructure.Authentication.Options;
using EstateHub.Infrastructure.Identity;
using EstateHub.Infrastructure.Identity.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EstateHub.Infrastructure.Authentication;

internal sealed class AuthenticationTokenService(
    EstateHubDbContext dbContext,
    IOptions<JwtOptions> jwtOptionsAccessor,
    TimeProvider timeProvider) : IAuthenticationTokenService
{
    private const int RefreshTokenByteLength = 64;

    private readonly EstateHubDbContext _dbContext = dbContext;
    private readonly JwtOptions _jwtOptions = jwtOptionsAccessor.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly JsonWebTokenHandler _tokenHandler = new();
    private readonly SigningCredentials _signingCredentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptionsAccessor.Value.SigningKey)),
        SecurityAlgorithms.HmacSha256);

    public async Task<AuthenticationTokenPair?> CreateSessionAsync(
        Guid applicationUserId,
        bool rememberMe,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                applicationUser => applicationUser.Id == applicationUserId,
                cancellationToken);

        if (!IsEligibleForTokenIssuance(user))
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        var refreshToken = CreateRefreshToken();
        var refreshTokenExpiresAt = rememberMe
            ? now.AddDays(_jwtOptions.RememberMeRefreshTokenDays)
            : now.AddDays(_jwtOptions.RefreshTokenDays);
        var tokenPair = CreateTokenPair(user, refreshToken, refreshTokenExpiresAt, now);

        _dbContext.RefreshSessions.Add(new RefreshSession
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = user.Id,
            TokenHash = HashRefreshToken(refreshToken),
            ExpiresAt = refreshTokenExpiresAt,
            RememberMe = rememberMe,
            CreatedAt = now,
            RevokedAt = null
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return tokenPair;
    }

    public async Task<AuthenticationTokenPair?> RotateRefreshTokenAsync(
        string? refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var tokenHash = HashRefreshToken(refreshToken);
        var now = _timeProvider.GetUtcNow();

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var existingSession = await _dbContext.RefreshSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                session => session.TokenHash == tokenHash,
                cancellationToken);

        if (existingSession is null
            || existingSession.RevokedAt is not null
            || existingSession.ExpiresAt <= now)
        {
            return null;
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                applicationUser => applicationUser.Id == existingSession.ApplicationUserId,
                cancellationToken);

        if (!IsEligibleForTokenIssuance(user))
        {
            return null;
        }

        var consumedSessionCount = await _dbContext.RefreshSessions
            .Where(session => session.Id == existingSession.Id
                && session.TokenHash == tokenHash
                && session.RevokedAt == null
                && session.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(session => session.RevokedAt, now),
                cancellationToken);

        if (consumedSessionCount != 1)
        {
            return null;
        }

        var replacementRefreshToken = CreateRefreshToken();

        _dbContext.RefreshSessions.Add(new RefreshSession
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = existingSession.ApplicationUserId,
            TokenHash = HashRefreshToken(replacementRefreshToken),
            ExpiresAt = existingSession.ExpiresAt,
            RememberMe = existingSession.RememberMe,
            CreatedAt = now,
            RevokedAt = null
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var tokenPair = CreateTokenPair(
            user,
            replacementRefreshToken,
            existingSession.ExpiresAt,
            now);

        await transaction.CommitAsync(cancellationToken);

        return tokenPair;
    }

    public async Task RevokeRefreshTokenAsync(
        string? refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = HashRefreshToken(refreshToken);
        var now = _timeProvider.GetUtcNow();

        await _dbContext.RefreshSessions
            .Where(session => session.TokenHash == tokenHash
                && session.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(session => session.RevokedAt, now),
                cancellationToken);
    }

    private AuthenticationTokenPair CreateTokenPair(
        ApplicationUser user,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAt,
        DateTimeOffset now)
    {
        var accessTokenExpiresAt = now.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var accessToken = CreateAccessToken(user, now, accessTokenExpiresAt);

        return new AuthenticationTokenPair(
            accessToken,
            accessTokenExpiresAt,
            refreshToken,
            refreshTokenExpiresAt);
    }

    private string CreateAccessToken(
        ApplicationUser user,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ]),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _signingCredentials
        };

        return _tokenHandler.CreateToken(descriptor);
    }

    private static bool IsEligibleForTokenIssuance(
        [NotNullWhen(true)] ApplicationUser? user)
    {
        return user is
        {
            AccountStatus: ApplicationUserAccountStatus.Active,
            EmailConfirmed: true
        }
        && !string.IsNullOrWhiteSpace(user.Email);
    }

    private static string CreateRefreshToken()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenByteLength));
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        return Convert.ToHexString(SHA256.HashData(tokenBytes));
    }
}
