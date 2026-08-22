using EstateHub.Application.Communications;
using EstateHub.Infrastructure.Email.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Sockets;

namespace EstateHub.Infrastructure.Email;

internal sealed class SmtpEmailSender(
    IOptions<SmtpEmailOptions> optionsAccessor,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpEmailOptions _options = optionsAccessor.Value;
    private readonly ILogger<SmtpEmailSender> _logger = logger;

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
            try
            {
                await smtpClient.ConnectAsync(
                    _options.Host,
                    _options.Port,
                    SecureSocketOptions.StartTls,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                LogFailure("connect", exception);
                throw;
            }

            try
            {
                await smtpClient.AuthenticateAsync(
                    _options.Username,
                    _options.Password,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                LogFailure("authenticate", exception);
                throw;
            }

            try
            {
                await smtpClient.SendAsync(message, cancellationToken);
            }
            catch (Exception exception)
            {
                LogFailure("send", exception);
                throw;
            }
        }
        finally
        {
            if (smtpClient.IsConnected)
            {
                try
                {
                    await smtpClient.DisconnectAsync(
                        quit: true,
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    LogFailure("disconnect", exception);
                    throw;
                }
            }
        }
    }

    private void LogFailure(string stage, Exception exception)
    {
        var smtpCommandException = exception as SmtpCommandException;
        var socketException = FindSocketException(exception);

        _logger.LogError(
            "SMTP operation failed. Stage: {Stage}; ExceptionType: {ExceptionType}; "
                + "SmtpErrorCode: {SmtpErrorCode}; SmtpStatusCode: {SmtpStatusCode}; "
                + "SocketErrorCode: {SocketErrorCode}",
            stage,
            exception.GetType().Name,
            smtpCommandException?.ErrorCode,
            smtpCommandException?.StatusCode,
            socketException?.SocketErrorCode);
    }

    private static SocketException? FindSocketException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SocketException socketException)
            {
                return socketException;
            }
        }

        return null;
    }
}
