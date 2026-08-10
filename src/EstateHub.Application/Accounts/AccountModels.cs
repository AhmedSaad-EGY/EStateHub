using EstateHub.Application.Authentication;

namespace EstateHub.Application.Accounts;

public enum CustomerRegistrationPersona
{
    Buyer,
    Renter,
    Agent
}

public sealed record RegisterCustomerCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    CustomerRegistrationPersona Persona,
    bool AcceptTerms);

public enum RegisterCustomerStatus
{
    Succeeded,
    Rejected,
    VerificationDeliveryFailed,
    ServiceUnavailable
}

public sealed record RegisterCustomerResult(RegisterCustomerStatus Status);

public sealed record ConfirmEmailCommand(Guid ApplicationUserId, string Code);

public enum ConfirmEmailStatus
{
    Succeeded,
    InvalidOrExpired
}

public sealed record ResendConfirmationEmailCommand(string Email);

public sealed record RequestPasswordResetCommand(string Email);

public sealed record ResetPasswordCommand(
    Guid ApplicationUserId,
    string Code,
    string NewPassword);

public enum ResetPasswordStatus
{
    Succeeded,
    Rejected,
    ServiceUnavailable
}

public sealed record LoginCommand(
    string Email,
    string Password,
    bool RememberMe);

public sealed record LoginResult(AuthenticationTokenPair? Tokens)
{
    public bool Succeeded => Tokens is not null;
}
