using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Infrastructure.Services;

public sealed class SmtpEmailSenderSettings
{
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 1025;
    public bool UseSsl { get; set; }
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

        using var client = new MailKit.Net.Smtp.SmtpClient();
        try
        {
            await client.ConnectAsync(config.SmtpHost, config.SmtpPort, config.UseSsl, ct);

            var mimeMessage = new MimeKit.MimeMessage();
            mimeMessage.From.Add(new MimeKit.MailboxAddress(config.FromName, config.FromAddress));
            mimeMessage.To.Add(MimeKit.MailboxAddress.Parse(to));
            mimeMessage.Subject = subject;
            mimeMessage.Body = new MimeKit.TextPart("html") { Text = htmlBody };

            await client.SendAsync(mimeMessage, ct);
            logger.LogDebug("Email sent to {Recipient}", to);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to send email to {Recipient}", to);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}
