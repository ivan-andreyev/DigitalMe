using DigitalMe.Services.Email.Models;

namespace DigitalMe.Services.Email;

/// <summary>
/// IMAP service interface for receiving and managing emails
/// Segregated interface following Interface Segregation Principle
/// </summary>
public interface IImapService
{
    /// <summary>
    /// Connect to IMAP server
    /// </summary>
    Task<bool> ConnectAsync();

    /// <summary>
    /// Disconnect from IMAP server
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Get list of available folders
    /// </summary>
    Task<IEnumerable<string>> GetFoldersAsync();

    /// <summary>
    /// Retrieve emails from specified folder
    /// </summary>
    Task<IEnumerable<EmailMessage>> GetEmailsAsync(EmailReceiveOptions options);

    /// <summary>
    /// Get unread emails
    /// </summary>
    Task<IEnumerable<EmailMessage>> GetUnreadEmailsAsync(string folderName = "INBOX");

    /// <summary>
    /// Search emails by criteria
    /// </summary>
    Task<IEnumerable<EmailMessage>> SearchAsync(EmailSearchCriteria criteria);

    /// <summary>
    /// Mark message as read/unread
    /// </summary>
    Task<bool> MarkAsReadAsync(string messageId, bool isRead = true);

    /// <summary>
    /// Delete message
    /// </summary>
    Task<bool> DeleteAsync(string messageId);

    /// <summary>
    /// Download attachment
    /// </summary>
    Task<byte[]> DownloadAttachmentAsync(string messageId, string attachmentId);

    /// <summary>
    /// Move message to folder
    /// </summary>
    Task<bool> MoveToFolderAsync(string messageId, string targetFolder);

    /// <summary>
    /// Get folder message count
    /// </summary>
    Task<(int total, int unread)> GetFolderStatsAsync(string folderName);
}