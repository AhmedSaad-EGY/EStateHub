using System.ComponentModel.DataAnnotations;
using EstateHub.Api.Contracts.Authentication;
using EstateHub.Api.RateLimiting;
using EstateHub.Application.Accounts;
using EstateHub.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EstateHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(
    IAccountService accountService,
    IAuthenticationTokenService authenticationTokenService) : ControllerBase
{
    private const int MaximumConfirmationCodeLength = 4096;
    private static readonly EmailAddressAttribute EmailAddressValidator = new();

    [HttpPost("register")]
    [EnableRateLimiting(AuthRateLimitPolicies.Registration)]
    public async Task<IActionResult> RegisterCustomer(
        RegisterCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var fullName = request.FullName?.Trim();
        var email = request.Email?.Trim();
        var phoneNumber = request.PhoneNumber?.Trim();
        var personaIsValid = TryParsePersona(request.Persona, out var persona);

        AddRequiredAndMaximumLengthError(
            nameof(request.FullName),
            fullName,
            maximumLength: 200);
        AddEmailErrors(nameof(request.Email), email);
        AddRequiredAndMaximumLengthError(
            nameof(request.PhoneNumber),
            phoneNumber,
            maximumLength: 32);

        if (string.IsNullOrEmpty(request.Password)
            || request.Password.Length is < 8 or > 128)
        {
            ModelState.AddModelError(
                nameof(request.Password),
                "Password is required and must contain between 8 and 128 characters.");
        }

        if (!string.Equals(
            request.Password,
            request.ConfirmPassword,
            StringComparison.Ordinal))
        {
            ModelState.AddModelError(
                nameof(request.ConfirmPassword),
                "ConfirmPassword must match Password.");
        }

        if (!personaIsValid)
        {
            ModelState.AddModelError(
                nameof(request.Persona),
                "Persona must be Buyer, Renter, or Agent.");
        }

        if (!request.AcceptTerms)
        {
            ModelState.AddModelError(
                nameof(request.AcceptTerms),
                "AcceptTerms must be true.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await accountService.RegisterCustomerAsync(
            new RegisterCustomerCommand(
                fullName!,
                email!,
                phoneNumber!,
                request.Password!,
                persona,
                request.AcceptTerms),
            cancellationToken);

        return result.Status switch
        {
            RegisterCustomerStatus.Succeeded => Accepted(),
            RegisterCustomerStatus.VerificationDeliveryFailed => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Verification email could not be delivered.",
                detail: "The account was created. Please use resend-confirmation to request another verification email."),
            RegisterCustomerStatus.ServiceUnavailable => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Registration is temporarily unavailable.",
                detail: "Please try again later."),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Registration could not be completed.",
                detail: "Review the supplied registration information and try again.")
        };
    }

    [HttpPost("confirm-email")]
    [EnableRateLimiting(AuthRateLimitPolicies.Verification)]
    public async Task<IActionResult> ConfirmEmail(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(request.UserId),
                "UserId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Code)
            || request.Code.Length > MaximumConfirmationCodeLength)
        {
            ModelState.AddModelError(
                nameof(request.Code),
                $"Code is required and must not exceed {MaximumConfirmationCodeLength} characters.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var status = await accountService.ConfirmEmailAsync(
            new ConfirmEmailCommand(request.UserId, request.Code!),
            cancellationToken);

        return status == ConfirmEmailStatus.Succeeded
            ? NoContent()
            : Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Email confirmation failed.",
                detail: "The confirmation link is invalid or has expired.");
    }

    [HttpPost("resend-confirmation")]
    [EnableRateLimiting(AuthRateLimitPolicies.EmailDelivery)]
    public async Task<IActionResult> ResendConfirmation(
        ResendConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim();
        AddEmailErrors(nameof(request.Email), email);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await accountService.ResendConfirmationEmailAsync(
            new ResendConfirmationEmailCommand(email!),
            cancellationToken);

        return Accepted();
    }

    [HttpPost("login")]
    [EnableRateLimiting(AuthRateLimitPolicies.Login)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim();
        AddEmailErrors(nameof(request.Email), email);

        if (string.IsNullOrEmpty(request.Password)
            || request.Password.Length > 128)
        {
            ModelState.AddModelError(
                nameof(request.Password),
                "Password is required and must not exceed 128 characters.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await accountService.LoginAsync(
            new LoginCommand(
                email!,
                request.Password!,
                request.RememberMe),
            cancellationToken);

        if (!result.Succeeded)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication failed.",
                detail: "The supplied credentials cannot be authenticated.");
        }

        Response.Headers.CacheControl = "no-store";

        return Ok(AuthenticationTokenResponse.From(result.Tokens!));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting(AuthRateLimitPolicies.TokenLifecycle)]
    public async Task<IActionResult> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)
            || request.RefreshToken.Length > 256)
        {
            return InvalidRefreshTokenProblem();
        }

        var tokens = await authenticationTokenService.RotateRefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        if (tokens is null)
        {
            return InvalidRefreshTokenProblem();
        }

        Response.Headers.CacheControl = "no-store";

        return Ok(AuthenticationTokenResponse.From(tokens));
    }

    [HttpPost("logout")]
    [EnableRateLimiting(AuthRateLimitPolicies.TokenLifecycle)]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RefreshToken is null
            || request.RefreshToken.Length <= 256)
        {
            await authenticationTokenService.RevokeRefreshTokenAsync(
                request.RefreshToken,
                cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting(AuthRateLimitPolicies.EmailDelivery)]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim();
        AddEmailErrors(nameof(request.Email), email);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await accountService.RequestPasswordResetAsync(
            new RequestPasswordResetCommand(email!),
            cancellationToken);

        return Accepted(new
        {
            message = "If an eligible account exists, a password reset email will be sent."
        });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(AuthRateLimitPolicies.Verification)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(request.UserId),
                "UserId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Code)
            || request.Code.Length > MaximumConfirmationCodeLength)
        {
            ModelState.AddModelError(
                nameof(request.Code),
                $"Code is required and must not exceed {MaximumConfirmationCodeLength} characters.");
        }

        if (string.IsNullOrEmpty(request.NewPassword)
            || request.NewPassword.Length is < 8 or > 128)
        {
            ModelState.AddModelError(
                nameof(request.NewPassword),
                "NewPassword is required and must contain between 8 and 128 characters.");
        }

        if (!string.Equals(
            request.NewPassword,
            request.ConfirmPassword,
            StringComparison.Ordinal))
        {
            ModelState.AddModelError(
                nameof(request.ConfirmPassword),
                "ConfirmPassword must match NewPassword.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var status = await accountService.ResetPasswordAsync(
            new ResetPasswordCommand(
                request.UserId,
                request.Code!,
                request.NewPassword!),
            cancellationToken);

        return status switch
        {
            ResetPasswordStatus.Succeeded => NoContent(),
            ResetPasswordStatus.ServiceUnavailable => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Password reset is temporarily unavailable.",
                detail: "Please try again later."),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Password reset could not be completed.",
                detail: "The reset link may be invalid or expired, or the new password may not satisfy the password requirements.")
        };
    }

    private void AddEmailErrors(string key, string? email)
    {
        if (string.IsNullOrWhiteSpace(email)
            || email.Length > 256
            || !EmailAddressValidator.IsValid(email))
        {
            ModelState.AddModelError(
                key,
                "A valid email address of at most 256 characters is required.");
        }
    }

    private ObjectResult InvalidRefreshTokenProblem()
    {
        return Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Refresh failed.",
            detail: "The refresh token cannot be accepted.");
    }

    private void AddRequiredAndMaximumLengthError(
        string key,
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > maximumLength)
        {
            ModelState.AddModelError(
                key,
                $"A value of at most {maximumLength} characters is required.");
        }
    }

    private static bool TryParsePersona(
        string? value,
        out CustomerRegistrationPersona persona)
    {
        var normalizedValue = value?.Trim();

        if (string.Equals(
            normalizedValue,
            "Buyer",
            StringComparison.OrdinalIgnoreCase))
        {
            persona = CustomerRegistrationPersona.Buyer;
            return true;
        }

        if (string.Equals(
            normalizedValue,
            "Renter",
            StringComparison.OrdinalIgnoreCase))
        {
            persona = CustomerRegistrationPersona.Renter;
            return true;
        }

        if (string.Equals(
            normalizedValue,
            "Agent",
            StringComparison.OrdinalIgnoreCase))
        {
            persona = CustomerRegistrationPersona.Agent;
            return true;
        }

        persona = default;
        return false;
    }
}
