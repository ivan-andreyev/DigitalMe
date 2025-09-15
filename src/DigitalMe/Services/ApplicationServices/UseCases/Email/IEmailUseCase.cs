using DigitalMe.Services.Email.Models;

namespace DigitalMe.Services.ApplicationServices.UseCases.Email;

/// <summary>
/// Email use case interface for application layer
/// Provides high-level email operations following Clean Architecture principles
/// </summary>
public interface IEmailUseCase
{
    /// <summary>
    /// Send a simple email
    /// </summary>
    Task<EmailSendResult> SendEmailAsync(string to, string subject, string body, bool isHtml = true);

    /// <summary>
    /// Send email with attachments
    /// </summary>
    Task<EmailSendResult> SendEmailWithAttachmentsAsync(string to, string subject, string body, IEnumerable<string> attachmentPaths, bool isHtml = true);

    /// <summary>
    /// Get recent unread emails
    /// </summary>
    Task<IEnumerable<EmailMessage>> GetRecentUnreadEmailsAsync(int maxCount = 10);

    /// <summary>
    /// Search emails by subject keyword
    /// </summary>
    Task<IEnumerable<EmailMessage>> SearchEmailsBySubjectAsync(string keyword, int maxResults = 20);

    /// <summary>
    /// Mark multiple emails as read
    /// </summary>
    Task<int> MarkEmailsAsReadAsync(IEnumerable<string> messageIds);

    /// <summary>
    /// Get email summary statistics
    /// </summary>
    Task<EmailSummary> GetEmailSummaryAsync();

    /// <summary>
    /// Test email service connectivity
    /// </summary>
    Task<EmailServiceStatus> TestEmailServiceAsync();
}

/// <summary>
/// Email summary information
/// </summary>
public class EmailSummary
{
    public int TotalEmails { get; set; }
    public int UnreadEmails { get; set; }
    public DateTime LastChecked { get; set; }
    public bool ServiceAvailable { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Email service status
/// </summary>
public class EmailServiceStatus
{
    public bool SmtpAvailable { get; set; }
    public bool ImapAvailable { get; set; }
    public string? SmtpError { get; set; }
    public string? ImapError { get; set; }
    public DateTime TestTime { get; set; } = DateTime.UtcNow;

    public bool IsFullyAvailable => SmtpAvailable && ImapAvailable;
}