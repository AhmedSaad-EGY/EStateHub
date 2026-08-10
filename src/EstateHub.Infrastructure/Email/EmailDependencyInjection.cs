using EstateHub.Application.Communications;
using EstateHub.Infrastructure.Email.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;

namespace EstateHub.Infrastructure.Email;

public static class EmailDependencyInjection
{
    public static IServiceCollection AddInfrastructureEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<SmtpEmailOptions>()
            .Bind(configuration.GetRequiredSection(SmtpEmailOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Host),
                $"{SmtpEmailOptions.SectionName}:Host is required.")
            .Validate(
                options => options.Port is >= 1 and <= 65535,
                $"{SmtpEmailOptions.SectionName}:Port must be between 1 and 65535.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Username),
                $"{SmtpEmailOptions.SectionName}:Username is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Password),
                $"{SmtpEmailOptions.SectionName}:Password is required.")
            .Validate(
                options => IsValidEmailAddress(options.FromAddress),
                $"{SmtpEmailOptions.SectionName}:FromAddress must be a valid email address.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.FromName),
                $"{SmtpEmailOptions.SectionName}:FromName is required.")
            .ValidateOnStart();

        services.AddTransient<IEmailSender, SmtpEmailSender>();

        return services;
    }

    private static bool IsValidEmailAddress(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && MailboxAddress.TryParse(value, out var address)
            && string.Equals(
                address.Address,
                value.Trim(),
                StringComparison.OrdinalIgnoreCase);
    }
}
