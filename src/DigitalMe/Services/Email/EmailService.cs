using DigitalMe.Services.Email.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalMe.Services.Email;

/// <summary>
/// Main email service implementation combining SMTP and IMAP functionality
/// Provides Ivan-Level email capabilities with comprehensive error handling
/// Following Clean Architecture patterns with dependency injection
/// </summary>
public class EmailService : IEmailService
{
    private readonly ISmtpService _smtpService;
    private readonly IImapService _imapService;
    private readonly ILogger<EmailService> _logger;
    private readonly EmailServiceConfig _config;

    public EmailService(
        ISmtpService smtpService,
        IImapService imapService,
        ILogger<EmailService> logger,
        IOptions<EmailServiceConfig> config)
    {
        _smtpService = smtpService;
        _imapService = imapService;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<EmailSendResult> SendEmailAsync(EmailMessage message)
    {
        try
        {
            _logger.LogInformation("Sending email to {To} with subject: {Subject}", message.To, message.Subject);
            return await _smtpService.SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", message.To);
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }

    public async Task<EmailSendResult> SendEmailWithAttachmentAsync(EmailMessage message, IEnumerable<EmailAttachment> attachments)
    {
        try
        {
            _logger.LogInformation("Sending email with attachments to {To} with subject: {Subject}", message.To, message.Subject);
            return await _smtpService.SendWithAttachmentsAsync(message, attachments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachments to {To}", message.To);
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }

    public async Task<IEnumerable<EmailMessage>> ReceiveEmailsAsync(EmailReceiveOptions options)
    {
        try
        {
            _logger.LogInformation("Retrieving emails from folder: {Folder}", options.FolderName);
            return await _imapService.GetEmailsAsync(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve emails from folder: {Folder}", options.FolderName);
            return Enumerable.Empty<EmailMessage>();
        }
    }

    public async Task<IEnumerable<EmailMessage>> GetUnreadEmailsAsync(string folderName = "INBOX")
    {
        try
        {
            _logger.LogInformation("Getting unread emails from folder: {Folder}", folderName);
            return await _imapService.GetUnreadEmailsAsync(folderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread emails from folder: {Folder}", folderName);
            return Enumerable.Empty<EmailMessage>();
        }
    }

    public async Task<bool> MarkEmailAsReadAsync(string messageId, bool isRead = true)
    {
        try
        {
            _logger.LogInformation("Marking email {MessageId} as {Status}", messageId, isRead ? "read" : "unread");
            return await _imapService.MarkAsReadAsync(messageId, isRead);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark email {MessageId} as {Status}", messageId, isRead ? "read" : "unread");
            return false;
        }
    }

    public async Task<bool> DeleteEmailAsync(string messageId)
    {
        try
        {
            _logger.LogInformation("Deleting email: {MessageId}", messageId);
            return await _imapService.DeleteAsync(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete email: {MessageId}", messageId);
            return false;
        }
    }

    public async Task<IEnumerable<EmailMessage>> SearchEmailsAsync(EmailSearchCriteria criteria)
    {
        try
        {
            _logger.LogInformation("Searching emails in folder: {Folder}", criteria.FolderName);
            return await _imapService.SearchAsync(criteria);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search emails in folder: {Folder}", criteria.FolderName);
            return Enumerable.Empty<EmailMessage>();
        }
    }

    public async Task<IEnumerable<string>> GetFoldersAsync()
    {
        try
        {
            _logger.LogInformation("Getting email folders");
            return await _imapService.GetFoldersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get email folders");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<byte[]> DownloadAttachmentAsync(string messageId, string attachmentId)
    {
        try
        {
            _logger.LogInformation("Downloading attachment {AttachmentId} from message {MessageId}", attachmentId, messageId);
            return await _imapService.DownloadAttachmentAsync(messageId, attachmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download attachment {AttachmentId} from message {MessageId}", attachmentId, messageId);
            return Array.Empty<byte>();
        }
    }

    public async Task<(int total, int unread)> GetFolderStatsAsync(string folderName)
    {
        try
        {
            _logger.LogInformation("Getting folder statistics for: {Folder}", folderName);
            return await _imapService.GetFolderStatsAsync(folderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get folder statistics for: {Folder}", folderName);
            return (0, 0);
        }
    }

    public async Task<bool> TestSmtpConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing SMTP connection");
            return await _smtpService.TestConnectionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP connection test failed");
            return false;
        }
    }

    public async Task<bool> TestImapConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing IMAP connection");
            var connected = await _imapService.ConnectAsync();
            if (connected)
            {
                await _imapService.DisconnectAsync();
            }
            return connected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IMAP connection test failed");
            return false;
        }
    }
}

/// <summary>
/// Email service configuration
/// </summary>
public class EmailServiceConfig
{
    public SmtpConfig Smtp { get; set; } = new();
    public ImapConfig Imap { get; set; } = new();
    public int DefaultTimeout { get; set; } = 30000; // 30 seconds
    public bool EnableSsl { get; set; } = true;
}

/// <summary>
/// SMTP configuration
/// </summary>
public class SmtpConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public int Timeout { get; set; } = 30000;
}

/// <summary>
/// IMAP configuration
/// </summary>
public class ImapConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 993;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public int Timeout { get; set; } = 30000;
}