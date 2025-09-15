using DigitalMe.Services.Email.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DigitalMe.Services.Email;

/// <summary>
/// IMAP service implementation using MailKit
/// Provides Ivan-Level email receiving and management capabilities
/// Following Clean Architecture patterns with dependency injection
/// </summary>
public class ImapService : IImapService, IDisposable
{
    private readonly ILogger<ImapService> _logger;
    private readonly ImapConfig _config;
    private ImapClient? _client;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ImapService(ILogger<ImapService> logger, IOptions<EmailServiceConfig> config)
    {
        _logger = logger;
        _config = config.Value.Imap;
    }

    public async Task<bool> ConnectAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_client?.IsConnected == true)
                return true;

            _client?.Dispose();
            _client = new ImapClient();
            _client.Timeout = _config.Timeout;

            await _client.ConnectAsync(_config.Host, _config.Port, _config.EnableSsl);
            await _client.AuthenticateAsync(_config.Username, _config.Password);

            _logger.LogInformation("Connected to IMAP server successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to IMAP server");
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_client?.IsConnected == true)
            {
                await _client.DisconnectAsync(true);
                _logger.LogInformation("Disconnected from IMAP server");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during IMAP disconnect");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<string>> GetFoldersAsync()
    {
        try
        {
            await EnsureConnectedAsync();

            var folders = await _client!.GetFoldersAsync(_client.PersonalNamespaces[0]);
            var folderNames = folders.Select(f => f.FullName).ToList();

            _logger.LogInformation("Retrieved {Count} folders", folderNames.Count);
            return folderNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get folders");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<IEnumerable<EmailMessage>> GetEmailsAsync(EmailReceiveOptions options)
    {
        try
        {
            await EnsureConnectedAsync();

            var folder = await _client!.GetFolderAsync(options.FolderName);
            await folder.OpenAsync(FolderAccess.ReadWrite);

            var query = SearchQuery.All;

            // Apply date filters
            if (options.Since.HasValue)
                query = query.And(SearchQuery.DeliveredAfter(options.Since.Value));

            if (options.Before.HasValue)
                query = query.And(SearchQuery.DeliveredBefore(options.Before.Value));

            var uids = await folder.SearchAsync(query);
            var messages = new List<EmailMessage>();

            // Limit results
            var limitedUids = uids.Take(options.MaxCount).ToList();

            foreach (var uid in limitedUids)
            {
                try
                {
                    var mimeMessage = await folder.GetMessageAsync(uid);
                    var emailMessage = ConvertFromMimeMessage(mimeMessage, uid.ToString());

                    if (options.IncludeAttachments)
                    {
                        emailMessage.Attachments = ExtractAttachments(mimeMessage).ToList();
                    }

                    messages.Add(emailMessage);

                    if (options.MarkAsRead)
                    {
                        await folder.AddFlagsAsync(uid, MessageFlags.Seen, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process message {Uid}", uid);
                }
            }

            await folder.CloseAsync();
            _logger.LogInformation("Retrieved {Count} emails from {Folder}", messages.Count, options.FolderName);
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get emails from folder {Folder}", options.FolderName);
            return Enumerable.Empty<EmailMessage>();
        }
    }

    public async Task<IEnumerable<EmailMessage>> GetUnreadEmailsAsync(string folderName = "INBOX")
    {
        try
        {
            await EnsureConnectedAsync();

            var folder = await _client!.GetFolderAsync(folderName);
            await folder.OpenAsync(FolderAccess.ReadWrite);

            var uids = await folder.SearchAsync(SearchQuery.NotSeen);
            var messages = new List<EmailMessage>();

            foreach (var uid in uids)
            {
                try
                {
                    var mimeMessage = await folder.GetMessageAsync(uid);
                    var emailMessage = ConvertFromMimeMessage(mimeMessage, uid.ToString());
                    messages.Add(emailMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process unread message {Uid}", uid);
                }
            }

            await folder.CloseAsync();
            _logger.LogInformation("Retrieved {Count} unread emails from {Folder}", messages.Count, folderName);
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread emails from folder {Folder}", folderName);
            return Enumerable.Empty<EmailMessage>();
        }
    }

    public async Task<IEnumerable<EmailMessage>> SearchAsync(EmailSearchCriteria criteria)
    {
        try
        {
            await EnsureConnectedAsync();

            var folder = await _client!.GetFolderAsync(criteria.FolderName);
            await folder.OpenAsync(FolderAccess.ReadOnly);

            var query = SearchQuery.All;

            // Build search query
            if (!string.IsNullOrEmpty(criteria.From))
                query = query.And(SearchQuery.FromContains(criteria.From));

            if (!string.IsNullOrEmpty(criteria.To))
                query = query.And(SearchQuery.ToContains(criteria.To));

            if (!string.IsNullOrEmpty(criteria.Subject))
                query = query.And(SearchQuery.SubjectContains(criteria.Subject));

            if (!string.IsNullOrEmpty(criteria.Body))
                query = query.And(SearchQuery.BodyContains(criteria.Body));

            if (criteria.SentSince.HasValue)
                query = query.And(SearchQuery.SentSince(criteria.SentSince.Value));

            if (criteria.SentBefore.HasValue)
                query = query.And(SearchQuery.SentBefore(criteria.SentBefore.Value));

            if (criteria.IsRead.HasValue)
            {
                query = criteria.IsRead.Value
                    ? query.And(SearchQuery.Seen)
                    : query.And(SearchQuery.NotSeen);
            }

            var uids = await folder.SearchAsync(query);
            var messages = new List<EmailMessage>();

            // Limit results
            var limitedUids = uids.Take(criteria.MaxResults).ToList();

            foreach (var uid in limitedUids)
            {
                try
                {
                    var mimeMessage = await folder.GetMessageAsync(uid);
                    var emailMessage = ConvertFromMimeMessage(mimeMessage, uid.ToString());
                    messages.Add(emailMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process search result message {Uid}", uid);
                }
            }

            await folder.CloseAsync();
            _logger.LogInformation("Found {Count} emails matching search criteria", messages.Count);
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search emails");
            return Enumerable.Empty<EmailMessage>();
        }
    }

    public async Task<bool> MarkAsReadAsync(string messageId, bool isRead = true)
    {
        try
        {
            await EnsureConnectedAsync();

            var inbox = await _client!.GetFolderAsync("INBOX");
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            // Find message by ID
            var uids = await inbox.SearchAsync(SearchQuery.HeaderContains("Message-ID", messageId));

            if (uids.Any())
            {
                var uid = uids.First();
                if (isRead)
                {
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
                }
                else
                {
                    await inbox.RemoveFlagsAsync(uid, MessageFlags.Seen, true);
                }

                await inbox.CloseAsync();
                _logger.LogInformation("Marked message {MessageId} as {Status}", messageId, isRead ? "read" : "unread");
                return true;
            }

            await inbox.CloseAsync();
            _logger.LogWarning("Message {MessageId} not found", messageId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark message {MessageId} as {Status}", messageId, isRead ? "read" : "unread");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string messageId)
    {
        try
        {
            await EnsureConnectedAsync();

            var inbox = await _client!.GetFolderAsync("INBOX");
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            var uids = await inbox.SearchAsync(SearchQuery.HeaderContains("Message-ID", messageId));

            if (uids.Any())
            {
                var uid = uids.First();
                await inbox.AddFlagsAsync(uid, MessageFlags.Deleted, true);
                await inbox.ExpungeAsync();

                await inbox.CloseAsync();
                _logger.LogInformation("Deleted message {MessageId}", messageId);
                return true;
            }

            await inbox.CloseAsync();
            _logger.LogWarning("Message {MessageId} not found for deletion", messageId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message {MessageId}", messageId);
            return false;
        }
    }

    public async Task<byte[]> DownloadAttachmentAsync(string messageId, string attachmentId)
    {
        try
        {
            await EnsureConnectedAsync();

            var inbox = await _client!.GetFolderAsync("INBOX");
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            var uids = await inbox.SearchAsync(SearchQuery.HeaderContains("Message-ID", messageId));

            if (uids.Any())
            {
                var uid = uids.First();
                var mimeMessage = await inbox.GetMessageAsync(uid);

                foreach (var attachment in mimeMessage.Attachments)
                {
                    if (attachment is MimePart part && part.ContentId == attachmentId)
                    {
                        using var memory = new MemoryStream();
                        await part.Content.DecodeToAsync(memory);

                        await inbox.CloseAsync();
                        _logger.LogInformation("Downloaded attachment {AttachmentId} from message {MessageId}", attachmentId, messageId);
                        return memory.ToArray();
                    }
                }
            }

            await inbox.CloseAsync();
            _logger.LogWarning("Attachment {AttachmentId} not found in message {MessageId}", attachmentId, messageId);
            return Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download attachment {AttachmentId} from message {MessageId}", attachmentId, messageId);
            return Array.Empty<byte>();
        }
    }

    public async Task<bool> MoveToFolderAsync(string messageId, string targetFolder)
    {
        try
        {
            await EnsureConnectedAsync();

            var inbox = await _client!.GetFolderAsync("INBOX");
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            var target = await _client.GetFolderAsync(targetFolder);

            var uids = await inbox.SearchAsync(SearchQuery.HeaderContains("Message-ID", messageId));

            if (uids.Any())
            {
                var uid = uids.First();
                await inbox.MoveToAsync(uid, target);

                await inbox.CloseAsync();
                _logger.LogInformation("Moved message {MessageId} to folder {TargetFolder}", messageId, targetFolder);
                return true;
            }

            await inbox.CloseAsync();
            _logger.LogWarning("Message {MessageId} not found for moving", messageId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move message {MessageId} to folder {TargetFolder}", messageId, targetFolder);
            return false;
        }
    }

    public async Task<(int total, int unread)> GetFolderStatsAsync(string folderName)
    {
        try
        {
            await EnsureConnectedAsync();

            var folder = await _client!.GetFolderAsync(folderName);
            await folder.OpenAsync(FolderAccess.ReadOnly);

            var total = folder.Count;
            var unreadUids = await folder.SearchAsync(SearchQuery.NotSeen);
            var unread = unreadUids.Count;

            await folder.CloseAsync();
            _logger.LogInformation("Folder {Folder} stats: {Total} total, {Unread} unread", folderName, total, unread);
            return (total, unread);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stats for folder {Folder}", folderName);
            return (0, 0);
        }
    }

    private async Task EnsureConnectedAsync()
    {
        if (_client == null || !_client.IsConnected)
        {
            await ConnectAsync();
        }
    }

    private EmailMessage ConvertFromMimeMessage(MimeMessage mimeMessage, string uid)
    {
        return new EmailMessage
        {
            MessageId = mimeMessage.MessageId ?? uid,
            From = mimeMessage.From.FirstOrDefault()?.ToString() ?? string.Empty,
            To = string.Join(", ", mimeMessage.To.Select(t => t.ToString())),
            Cc = string.Join(", ", mimeMessage.Cc.Select(c => c.ToString())),
            Subject = mimeMessage.Subject ?? string.Empty,
            Body = mimeMessage.TextBody ?? mimeMessage.HtmlBody ?? string.Empty,
            IsHtml = !string.IsNullOrEmpty(mimeMessage.HtmlBody),
            SentDate = mimeMessage.Date.DateTime,
            ReceivedDate = DateTime.UtcNow,
            IsRead = false, // Will be determined by message flags
            HasAttachments = mimeMessage.Attachments.Any(),
            Priority = mimeMessage.Priority switch
            {
                MessagePriority.Urgent => EmailPriority.High,
                MessagePriority.NonUrgent => EmailPriority.Low,
                _ => EmailPriority.Normal
            },
            InReplyTo = mimeMessage.InReplyTo,
            References = string.Join(", ", mimeMessage.References)
        };
    }

    private IEnumerable<EmailAttachment> ExtractAttachments(MimeMessage mimeMessage)
    {
        foreach (var attachment in mimeMessage.Attachments)
        {
            if (attachment is MimePart part)
            {
                using var memory = new MemoryStream();
                part.Content.DecodeTo(memory);

                yield return new EmailAttachment
                {
                    Id = part.ContentId ?? Guid.NewGuid().ToString(),
                    FileName = part.FileName ?? "attachment",
                    ContentType = part.ContentType.MimeType,
                    Size = memory.Length,
                    Content = memory.ToArray(),
                    IsInline = part.IsAttachment == false
                };
            }
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}