using EstateHub.Application.Communications;
using EstateHub.Infrastructure.Email.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EstateHub.Infrastructure.Email;

internal sealed class SmtpEmailSender(
    IOptions<SmtpEmailOptions> optionsAccessor) : IEmailSender
{
    private readonly SmtpEmailOptions _options = optionsAccessor.Value;

    public async Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

        if (!MailboxAddress.TryParse(recipientEmail, out var recipient)
            || !string.Equals(
                recipient.Address,
                recipientEmail.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The recipient email address is invalid.",
                nameof(recipientEmail));
        }

        var message = new MimeMessage
        {
            Subject = subject,
            Body = new BodyBuilder
            {
                HtmlBody = htmlBody
            }.ToMessageBody()
        };

        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(recipient);

        using var smtpClient = new SmtpClient();

        try
        {
            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);
            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);
            await smtpClient.SendAsync(message, cancellationToken);
        }
        finally
        {
            if (smtpClient.IsConnected)
            {
                await smtpClient.DisconnectAsync(
                    quit: true,
                    cancellationToken);
            }
        }
    }
}
