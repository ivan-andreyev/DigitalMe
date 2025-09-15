using DigitalMe.Services.Email.Models;

namespace DigitalMe.Services.Email;

/// <summary>
/// Main interface for email operations providing Ivan-Level email capabilities
/// Combines SMTP sending and IMAP receiving functionality
/// Following Clean Architecture patterns with dependency injection
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send an email message via SMTP
    /// </summary>
    Task<EmailSendResult> SendEmailAsync(EmailMessage message);

    /// <summary>
    /// Send an email with attachment via SMTP
    /// </summary>
    Task<EmailSendResult> SendEmailWithAttachmentAsync(EmailMessage message, IEnumerable<EmailAttachment> attachments);

    /// <summary>
    /// Retrieve emails from IMAP server
    /// </summary>
    Task<IEnumerable<EmailMessage>> ReceiveEmailsAsync(EmailReceiveOptions options);

    /// <summary>
    /// Get unread emails from IMAP server
    /// </summary>
    Task<IEnumerable<EmailMessage>> GetUnreadEmailsAsync(string folderName = "INBOX");

    /// <summary>
    /// Mark email as read/unread
    /// </summary>
    Task<bool> MarkEmailAsReadAsync(string messageId, bool isRead = true);

    /// <summary>
    /// Delete email from server
    /// </summary>
    Task<bool> DeleteEmailAsync(string messageId);

    /// <summary>
    /// Search emails by criteria
    /// </summary>
    Task<IEnumerable<EmailMessage>> SearchEmailsAsync(EmailSearchCriteria criteria);

    /// <summary>
    /// Get email folders/mailboxes
    /// </summary>
    Task<IEnumerable<string>> GetFoldersAsync();

    /// <summary>
    /// Download email attachment
    /// </summary>
    Task<byte[]> DownloadAttachmentAsync(string messageId, string attachmentId);

    /// <summary>
    /// Get folder statistics (total and unread count)
    /// </summary>
    Task<(int total, int unread)> GetFolderStatsAsync(string folderName);

    /// <summary>
    /// Test SMTP connection
    /// </summary>
    Task<bool> TestSmtpConnectionAsync();

    /// <summary>
    /// Test IMAP connection
    /// </summary>
    Task<bool> TestImapConnectionAsync();
}