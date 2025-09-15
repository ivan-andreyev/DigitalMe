using DigitalMe.Services.Email;
using DigitalMe.Services.Email.Models;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.UseCases.Email;

/// <summary>
/// Email use case implementation for application layer
/// Provides high-level email operations following Clean Architecture principles
/// Focuses solely on email business logic without infrastructure concerns
/// </summary>
public class EmailUseCase : IEmailUseCase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailUseCase> _logger;

    public EmailUseCase(
        IEmailService emailService,
        ILogger<EmailUseCase> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);

        var message = new EmailMessage
        {
            To = to,
            Subject = subject,
            Body = body,
            IsHtml = isHtml,
            Priority = EmailPriority.Normal
        };

        return await _emailService.SendEmailAsync(message);
    }

    public async Task<EmailSendResult> SendEmailWithAttachmentsAsync(string to, string subject, string body, IEnumerable<string> attachmentPaths, bool isHtml = true)
    {
        _logger.LogInformation("Sending email with attachments to {To}", to);

        var message = new EmailMessage
        {
            To = to,
            Subject = subject,
            Body = body,
            IsHtml = isHtml,
            Priority = EmailPriority.Normal
        };

        var attachments = new List<EmailAttachment>();

        foreach (var path in attachmentPaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    var content = await File.ReadAllBytesAsync(path);
                    var fileName = Path.GetFileName(path);
                    var contentType = GetContentType(path);

                    attachments.Add(new EmailAttachment
                    {
                        Id = Guid.NewGuid().ToString(),
                        FileName = fileName,
                        ContentType = contentType,
                        Content = content,
                        Size = content.Length,
                        IsInline = false
                    });
                }
                else
                {
                    _logger.LogWarning("Attachment file not found: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read attachment file: {Path}", path);
            }
        }

        return await _emailService.SendEmailWithAttachmentAsync(message, attachments);
    }

    public async Task<IEnumerable<EmailMessage>> GetRecentUnreadEmailsAsync(int maxCount = 10)
    {
        _logger.LogInformation("Getting recent unread emails (max: {MaxCount})", maxCount);

        var options = new EmailReceiveOptions
        {
            FolderName = "INBOX",
            MaxCount = maxCount,
            IncludeAttachments = false,
            MarkAsRead = false,
            Since = DateTime.UtcNow.AddDays(-7) // Last 7 days
        };

        var allEmails = await _emailService.ReceiveEmailsAsync(options);
        return allEmails.Where(e => !e.IsRead).OrderByDescending(e => e.ReceivedDate);
    }

    public async Task<IEnumerable<EmailMessage>> SearchEmailsBySubjectAsync(string keyword, int maxResults = 20)
    {
        _logger.LogInformation("Searching emails by subject keyword: {Keyword}", keyword);

        var criteria = new EmailSearchCriteria
        {
            Subject = keyword,
            FolderName = "INBOX",
            MaxResults = maxResults
        };

        return await _emailService.SearchEmailsAsync(criteria);
    }

    public async Task<int> MarkEmailsAsReadAsync(IEnumerable<string> messageIds)
    {
        _logger.LogInformation("Marking {Count} emails as read", messageIds.Count());

        int markedCount = 0;

        foreach (var messageId in messageIds)
        {
            try
            {
                var success = await _emailService.MarkEmailAsReadAsync(messageId, true);
                if (success)
                {
                    markedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark email {MessageId} as read", messageId);
            }
        }

        _logger.LogInformation("Successfully marked {MarkedCount} out of {TotalCount} emails as read", markedCount, messageIds.Count());
        return markedCount;
    }

    public async Task<EmailSummary> GetEmailSummaryAsync()
    {
        _logger.LogInformation("Getting email summary");

        try
        {
            var (total, unread) = await _emailService.GetFolderStatsAsync("INBOX");

            return new EmailSummary
            {
                TotalEmails = total,
                UnreadEmails = unread,
                LastChecked = DateTime.UtcNow,
                ServiceAvailable = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get email summary");

            return new EmailSummary
            {
                TotalEmails = 0,
                UnreadEmails = 0,
                LastChecked = DateTime.UtcNow,
                ServiceAvailable = false,
                LastError = ex.Message
            };
        }
    }

    public async Task<EmailServiceStatus> TestEmailServiceAsync()
    {
        _logger.LogInformation("Testing email service connectivity");

        var status = new EmailServiceStatus();

        // Test email service connectivity
        try
        {
            status.SmtpAvailable = await _emailService.TestSmtpConnectionAsync();
            status.ImapAvailable = await _emailService.TestImapConnectionAsync();
        }
        catch (Exception ex)
        {
            status.SmtpAvailable = false;
            status.ImapAvailable = false;
            status.SmtpError = ex.Message;
            status.ImapError = ex.Message;
            _logger.LogError(ex, "Email service connection test failed");
        }

        _logger.LogInformation("Email service test completed. SMTP: {SmtpStatus}, IMAP: {ImapStatus}",
            status.SmtpAvailable ? "OK" : "FAILED",
            status.ImapAvailable ? "OK" : "FAILED");

        return status;
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".txt" => "text/plain",
            ".html" => "text/html",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            _ => "application/octet-stream"
        };
    }
}