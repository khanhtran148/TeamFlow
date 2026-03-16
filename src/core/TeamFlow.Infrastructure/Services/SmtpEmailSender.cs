using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Infrastructure.Services;

public sealed class SmtpEmailSenderSettings
{
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 1025;
    public string FromAddress { get; set; } = "noreply@teamflow.local";
    public string FromName { get; set; } = "TeamFlow";
}

/// <summary>
/// Sends emails via SMTP. Implementation uses MailKit.
/// Note: Human review required per CLAUDE.md rules for email sending.
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<SmtpEmailSenderSettings> settings,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var config = settings.Value;

        using var message = new MailKit.Net.Smtp.SmtpClient();
        try
        {
            await message.ConnectAsync(config.SmtpHost, config.SmtpPort, false, ct);

            var mimeMessage = new MimeKit.MimeMessage();
            mimeMessage.From.Add(new MimeKit.MailboxAddress(config.FromName, config.FromAddress));
            mimeMessage.To.Add(MimeKit.MailboxAddress.Parse(to));
            mimeMessage.Subject = subject;
            mimeMessage.Body = new MimeKit.TextPart("html") { Text = htmlBody };

            await message.SendAsync(mimeMessage, ct);
            logger.LogInformation("Email sent to {Recipient} with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Recipient}", to);
            throw;
        }
        finally
        {
            await message.DisconnectAsync(true, ct);
        }
    }
}
