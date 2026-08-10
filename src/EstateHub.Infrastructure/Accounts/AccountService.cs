using System.Text;
using System.Text.Encodings.Web;
using EstateHub.Application.Accounts;
using EstateHub.Application.Authentication;
using EstateHub.Application.Communications;
using EstateHub.Domain.Entities.Users;
using EstateHub.Infrastructure.Accounts.Options;
using EstateHub.Infrastructure.Identity;
using EstateHub.Infrastructure.Identity.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DomainCustomerPersona = EstateHub.Domain.Enums.CustomerPersona;

namespace EstateHub.Infrastructure.Accounts;

internal sealed class AccountService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    EstateHubDbContext dbContext,
    IAuthenticationTokenService authenticationTokenService,
    IEmailSender emailSender,
    IOptions<LegalPolicyOptions> legalPolicyOptionsAccessor,
    IOptions<FrontendOptions> frontendOptionsAccessor,
    TimeProvider timeProvider) : IAccountService
{
    private const int MaximumConfirmationCodeLength = 4096;
    private const string TermsAndConditionsPolicyType = "TermsAndConditions";
    private const string PrivacyPolicyType = "PrivacyPolicy";
    private const string WebConsentSource = "Web";
    private const string ConfirmationEmailSubject = "Verify your EstateHub email";
    private const string PasswordResetEmailSubject = "Reset your EstateHub password";

    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly EstateHubDbContext _dbContext = dbContext;
    private readonly IAuthenticationTokenService _authenticationTokenService = authenticationTokenService;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly LegalPolicyOptions _legalPolicyOptions = legalPolicyOptionsAccessor.Value;
    private readonly FrontendOptions _frontendOptions = frontendOptionsAccessor.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<RegisterCustomerResult> RegisterCustomerAsync(
        RegisterCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!IsValidRegistration(command))
        {
            return new RegisterCustomerResult(RegisterCustomerStatus.Rejected);
        }

        var fullName = command.FullName.Trim();
        var email = command.Email.Trim();
        var phoneNumber = command.PhoneNumber.Trim();
        var registeredAt = _timeProvider.GetUtcNow();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            PhoneNumber = phoneNumber,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            AccountStatus = ApplicationUserAccountStatus.Active,
            PreferredLanguage = "en",
            CreatedAt = registeredAt,
            UpdatedAt = registeredAt
        };

        await using (var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken))
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var identityResult = await _userManager.CreateAsync(user, command.Password);

                cancellationToken.ThrowIfCancellationRequested();

                if (!identityResult.Succeeded)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return new RegisterCustomerResult(RegisterCustomerStatus.Rejected);
                }

                _dbContext.Set<CustomerProfile>().Add(new CustomerProfile
                {
                    Id = Guid.NewGuid(),
                    ApplicationUserId = user.Id,
                    FullName = fullName,
                    Persona = MapPersona(command.Persona),
                    CreatedAt = registeredAt,
                    UpdatedAt = registeredAt
                });

                _dbContext.Set<UserConsent>().AddRange(
                    CreateConsent(
                        user.Id,
                        TermsAndConditionsPolicyType,
                        _legalPolicyOptions.TermsAndConditionsVersion,
                        registeredAt),
                    CreateConsent(
                        user.Id,
                        PrivacyPolicyType,
                        _legalPolicyOptions.PrivacyPolicyVersion,
                        registeredAt));

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new RegisterCustomerResult(RegisterCustomerStatus.ServiceUnavailable);
            }
        }

        try
        {
            await SendConfirmationEmailAsync(user, cancellationToken);
            return new RegisterCustomerResult(RegisterCustomerStatus.Succeeded);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new RegisterCustomerResult(
                RegisterCustomerStatus.VerificationDeliveryFailed);
        }
    }

    public async Task<ConfirmEmailStatus> ConfirmEmailAsync(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ApplicationUserId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.Code)
            || command.Code.Length > MaximumConfirmationCodeLength)
        {
            return ConfirmEmailStatus.InvalidOrExpired;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(
            command.ApplicationUserId.ToString());

        cancellationToken.ThrowIfCancellationRequested();

        if (user is null)
        {
            return ConfirmEmailStatus.InvalidOrExpired;
        }

        if (user.EmailConfirmed)
        {
            return ConfirmEmailStatus.Succeeded;
        }

        string identityToken;

        try
        {
            identityToken = StrictUtf8.GetString(
                WebEncoders.Base64UrlDecode(command.Code.Trim()));
        }
        catch (Exception exception) when (
            exception is FormatException or DecoderFallbackException)
        {
            return ConfirmEmailStatus.InvalidOrExpired;
        }

        var identityResult = await _userManager.ConfirmEmailAsync(
            user,
            identityToken);

        cancellationToken.ThrowIfCancellationRequested();

        return identityResult.Succeeded
            ? ConfirmEmailStatus.Succeeded
            : ConfirmEmailStatus.InvalidOrExpired;
    }

    public async Task ResendConfirmationEmailAsync(
        ResendConfirmationEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(command.Email.Trim());

        cancellationToken.ThrowIfCancellationRequested();

        if (user is null
            || user.EmailConfirmed
            || user.AccountStatus != ApplicationUserAccountStatus.Active
            || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        try
        {
            await SendConfirmationEmailAsync(user, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // The caller always receives the same accepted outcome to prevent enumeration.
        }
    }

    public async Task RequestPasswordResetAsync(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userManager.FindByEmailAsync(command.Email.Trim());

            cancellationToken.ThrowIfCancellationRequested();

            if (user is null
                || !user.EmailConfirmed
                || user.AccountStatus != ApplicationUserAccountStatus.Active
                || string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            await SendPasswordResetEmailAsync(user, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // The caller always receives the same accepted outcome to prevent enumeration.
        }
    }

    public async Task<ResetPasswordStatus> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ApplicationUserId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.Code)
            || command.Code.Length > MaximumConfirmationCodeLength
            || string.IsNullOrEmpty(command.NewPassword)
            || command.NewPassword.Length is < 8 or > 128)
        {
            return ResetPasswordStatus.Rejected;
        }

        string identityToken;

        try
        {
            identityToken = StrictUtf8.GetString(
                WebEncoders.Base64UrlDecode(command.Code.Trim()));
        }
        catch (Exception exception) when (
            exception is FormatException or DecoderFallbackException)
        {
            return ResetPasswordStatus.Rejected;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userManager.FindByIdAsync(
                command.ApplicationUserId.ToString());

            cancellationToken.ThrowIfCancellationRequested();

            if (user is null
                || !user.EmailConfirmed
                || user.AccountStatus != ApplicationUserAccountStatus.Active)
            {
                return ResetPasswordStatus.Rejected;
            }

            var revokedAt = _timeProvider.GetUtcNow();

            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            try
            {
                var resetResult = await _userManager.ResetPasswordAsync(
                    user,
                    identityToken,
                    command.NewPassword);

                cancellationToken.ThrowIfCancellationRequested();

                if (!resetResult.Succeeded)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return ResetPasswordStatus.Rejected;
                }

                var failedCountResult = await _userManager
                    .ResetAccessFailedCountAsync(user);

                cancellationToken.ThrowIfCancellationRequested();

                if (!failedCountResult.Succeeded)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return ResetPasswordStatus.ServiceUnavailable;
                }

                var lockoutResult = await _userManager.SetLockoutEndDateAsync(
                    user,
                    lockoutEnd: null);

                cancellationToken.ThrowIfCancellationRequested();

                if (!lockoutResult.Succeeded)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return ResetPasswordStatus.ServiceUnavailable;
                }

                await _dbContext.RefreshSessions
                    .Where(session => session.ApplicationUserId == user.Id
                        && session.RevokedAt == null)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            session => session.RevokedAt,
                            revokedAt),
                        cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return ResetPasswordStatus.Succeeded;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return ResetPasswordStatus.ServiceUnavailable;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ResetPasswordStatus.ServiceUnavailable;
        }
    }

    public async Task<LoginResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email)
            || string.IsNullOrEmpty(command.Password))
        {
            return new LoginResult(null);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(command.Email.Trim());

        cancellationToken.ThrowIfCancellationRequested();

        if (user is null)
        {
            return new LoginResult(null);
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            command.Password,
            lockoutOnFailure: true);

        cancellationToken.ThrowIfCancellationRequested();

        if (!signInResult.Succeeded)
        {
            return new LoginResult(null);
        }

        var tokens = await _authenticationTokenService.CreateSessionAsync(
            user.Id,
            command.RememberMe,
            cancellationToken);

        return new LoginResult(tokens);
    }

    private async Task SendConfirmationEmailAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var identityToken = await _userManager
            .GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(identityToken));
        var confirmationUrl = CreateConfirmationUrl(user.Id, encodedToken);
        var encodedConfirmationUrl = HtmlEncoder.Default.Encode(confirmationUrl);
        var htmlBody = $"""
            <p>Welcome to EstateHub.</p>
            <p><a href="{encodedConfirmationUrl}">Verify your email address</a></p>
            """;

        await _emailSender.SendAsync(
            user.Email!,
            ConfirmationEmailSubject,
            htmlBody,
            cancellationToken);
    }

    private async Task SendPasswordResetEmailAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var identityToken = await _userManager
            .GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(identityToken));
        var resetUrl = CreatePasswordResetUrl(user.Id, encodedToken);
        var encodedResetUrl = HtmlEncoder.Default.Encode(resetUrl);
        var htmlBody = $"""
            <p>A password reset was requested for your EstateHub account.</p>
            <p><a href="{encodedResetUrl}">Reset your password</a></p>
            """;

        await _emailSender.SendAsync(
            user.Email!,
            PasswordResetEmailSubject,
            htmlBody,
            cancellationToken);
    }

    private string CreateConfirmationUrl(Guid applicationUserId, string encodedToken)
    {
        var frontendBaseUri = new Uri(_frontendOptions.BaseUrl, UriKind.Absolute);
        var verificationUri = new Uri(frontendBaseUri, "/verify-email");

        return QueryHelpers.AddQueryString(
            verificationUri.AbsoluteUri,
            new Dictionary<string, string?>
            {
                ["userId"] = applicationUserId.ToString(),
                ["code"] = encodedToken
            });
    }

    private string CreatePasswordResetUrl(Guid applicationUserId, string encodedToken)
    {
        var frontendBaseUri = new Uri(_frontendOptions.BaseUrl, UriKind.Absolute);
        var resetPasswordUri = new Uri(frontendBaseUri, "/reset-password");

        return QueryHelpers.AddQueryString(
            resetPasswordUri.AbsoluteUri,
            new Dictionary<string, string?>
            {
                ["userId"] = applicationUserId.ToString(),
                ["code"] = encodedToken
            });
    }

    private static bool IsValidRegistration(RegisterCustomerCommand command)
    {
        return command.AcceptTerms
            && !string.IsNullOrWhiteSpace(command.FullName)
            && command.FullName.Trim().Length <= 200
            && !string.IsNullOrWhiteSpace(command.Email)
            && command.Email.Trim().Length <= 256
            && !string.IsNullOrWhiteSpace(command.PhoneNumber)
            && command.PhoneNumber.Trim().Length <= 32
            && !string.IsNullOrEmpty(command.Password)
            && command.Password.Length is >= 8 and <= 128
            && Enum.IsDefined(command.Persona);
    }

    private static DomainCustomerPersona MapPersona(
        CustomerRegistrationPersona persona)
    {
        return persona switch
        {
            CustomerRegistrationPersona.Buyer => DomainCustomerPersona.Buyer,
            CustomerRegistrationPersona.Renter => DomainCustomerPersona.Renter,
            CustomerRegistrationPersona.Agent => DomainCustomerPersona.Agent,
            _ => throw new ArgumentOutOfRangeException(nameof(persona))
        };
    }

    private static UserConsent CreateConsent(
        Guid applicationUserId,
        string policyType,
        string policyVersion,
        DateTimeOffset acceptedAt)
    {
        return new UserConsent
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = applicationUserId,
            PolicyType = policyType,
            PolicyVersion = policyVersion,
            AcceptedAt = acceptedAt,
            ConsentSource = WebConsentSource
        };
    }
}
