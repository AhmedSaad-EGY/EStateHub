namespace EstateHub.Application.Accounts;

public interface IAccountService
{
    Task<RegisterCustomerResult> RegisterCustomerAsync(
        RegisterCustomerCommand command,
        CancellationToken cancellationToken = default);

    Task<ConfirmEmailStatus> ConfirmEmailAsync(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken = default);

    Task ResendConfirmationEmailAsync(
        ResendConfirmationEmailCommand command,
        CancellationToken cancellationToken = default);

    Task RequestPasswordResetAsync(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken = default);

    Task<ResetPasswordStatus> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<LoginResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default);
}
