using DigitalMe.Services.Email.Models;

namespace DigitalMe.Services.Email;

/// <summary>
/// SMTP service interface for sending emails
/// Segregated interface following Interface Segregation Principle
/// </summary>
public interface ISmtpService
{
    /// <summary>
    /// Send email message via SMTP
    /// </summary>
    Task<EmailSendResult> SendAsync(EmailMessage message);

    /// <summary>
    /// Send email with attachments via SMTP
    /// </summary>
    Task<EmailSendResult> SendWithAttachmentsAsync(EmailMessage message, IEnumerable<EmailAttachment> attachments);

    /// <summary>
    /// Test SMTP connection
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Send bulk emails (with rate limiting)
    /// </summary>
    Task<IEnumerable<EmailSendResult>> SendBulkAsync(IEnumerable<EmailMessage> messages);
}