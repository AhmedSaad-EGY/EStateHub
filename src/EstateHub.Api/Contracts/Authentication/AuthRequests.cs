namespace EstateHub.Api.Contracts.Authentication;

public sealed class RegisterCustomerRequest
{
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Password { get; init; }
    public string? ConfirmPassword { get; init; }
    public string? Persona { get; init; }
    public bool AcceptTerms { get; init; }
}

public sealed class ConfirmEmailRequest
{
    public Guid UserId { get; init; }
    public string? Code { get; init; }
}

public sealed class ResendConfirmationRequest
{
    public string? Email { get; init; }
}

public sealed class LoginRequest
{
    public string? Email { get; init; }
    public string? Password { get; init; }
    public bool RememberMe { get; init; }
}

public sealed class RefreshTokenRequest
{
    public string? RefreshToken { get; init; }
}

public sealed class ForgotPasswordRequest
{
    public string? Email { get; init; }
}

public sealed class ResetPasswordRequest
{
    public Guid UserId { get; init; }
    public string? Code { get; init; }
    public string? NewPassword { get; init; }
    public string? ConfirmPassword { get; init; }
}
