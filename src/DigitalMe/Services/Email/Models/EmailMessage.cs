namespace DigitalMe.Services.Email.Models;

/// <summary>
/// Represents an email message for sending or receiving
/// Used across SMTP and IMAP operations
/// </summary>
public class EmailMessage
{
    public string? MessageId { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public DateTime? SentDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public bool IsRead { get; set; }
    public bool HasAttachments { get; set; }
    public List<EmailAttachment> Attachments { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public string? InReplyTo { get; set; }
    public string? References { get; set; }
}

/// <summary>
/// Email attachment representation
/// </summary>
public class EmailAttachment
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public byte[]? Content { get; set; }
    public bool IsInline { get; set; }
}

/// <summary>
/// Email priority levels
/// </summary>
public enum EmailPriority
{
    Low = 1,
    Normal = 3,
    High = 5
}

/// <summary>
/// Result of email sending operation
/// </summary>
public class EmailSendResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Options for receiving emails
/// </summary>
public class EmailReceiveOptions
{
    public string FolderName { get; set; } = "INBOX";
    public int MaxCount { get; set; } = 50;
    public bool IncludeAttachments { get; set; } = false;
    public bool MarkAsRead { get; set; } = false;
    public DateTime? Since { get; set; }
    public DateTime? Before { get; set; }
}

/// <summary>
/// Email search criteria
/// </summary>
public class EmailSearchCriteria
{
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public DateTime? SentSince { get; set; }
    public DateTime? SentBefore { get; set; }
    public bool? IsRead { get; set; }
    public bool? HasAttachments { get; set; }
    public string FolderName { get; set; } = "INBOX";
    public int MaxResults { get; set; } = 100;
}