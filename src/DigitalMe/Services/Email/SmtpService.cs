using DigitalMe.Services.Email.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DigitalMe.Services.Email;

/// <summary>
/// SMTP service implementation using MailKit
/// Provides Ivan-Level email sending capabilities with comprehensive error handling
/// Following Clean Architecture patterns with dependency injection
/// </summary>
public class SmtpService : ISmtpService, IDisposable
{
    private readonly ILogger<SmtpService> _logger;
    private readonly SmtpConfig _config;
    private SmtpClient? _client;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public SmtpService(ILogger<SmtpService> logger, IOptions<EmailServiceConfig> config)
    {
        _logger = logger;
        _config = config.Value.Smtp;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message)
    {
        try
        {
            var mimeMessage = ConvertToMimeMessage(message);
            return await SendMimeMessageAsync(mimeMessage, message.To);
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

    public async Task<EmailSendResult> SendWithAttachmentsAsync(EmailMessage message, IEnumerable<EmailAttachment> attachments)
    {
        try
        {
            var mimeMessage = ConvertToMimeMessage(message);

            // Add attachments
            var multipart = new Multipart("mixed");

            // Add the body
            if (message.IsHtml)
            {
                multipart.Add(new TextPart("html") { Text = message.Body });
            }
            else
            {
                multipart.Add(new TextPart("plain") { Text = message.Body });
            }

            // Add attachments
            foreach (var attachment in attachments)
            {
                if (attachment.Content != null)
                {
                    var part = new MimePart(attachment.ContentType)
                    {
                        Content = new MimeContent(new MemoryStream(attachment.Content)),
                        ContentDisposition = new ContentDisposition(attachment.IsInline ? "inline" : "attachment"),
                        FileName = attachment.FileName
                    };

                    if (!string.IsNullOrEmpty(attachment.Id))
                    {
                        part.ContentId = attachment.Id;
                    }

                    multipart.Add(part);
                }
            }

            mimeMessage.Body = multipart;

            return await SendMimeMessageAsync(mimeMessage, message.To);
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

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var client = new SmtpClient();
            client.Timeout = _config.Timeout;

            await client.ConnectAsync(_config.Host, _config.Port, _config.EnableSsl);
            await client.AuthenticateAsync(_config.Username, _config.Password);
            await client.DisconnectAsync(true);

            _logger.LogInformation("SMTP connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP connection test failed");
            return false;
        }
    }

    public async Task<IEnumerable<EmailSendResult>> SendBulkAsync(IEnumerable<EmailMessage> messages)
    {
        var results = new List<EmailSendResult>();

        await _semaphore.WaitAsync();
        try
        {
            await EnsureConnectedAsync();

            foreach (var message in messages)
            {
                try
                {
                    var mimeMessage = ConvertToMimeMessage(message);
                    await _client!.SendAsync(mimeMessage);

                    results.Add(new EmailSendResult
                    {
                        Success = true,
                        MessageId = mimeMessage.MessageId,
                        SentAt = DateTime.UtcNow
                    });

                    _logger.LogInformation("Bulk email sent successfully to {To}", message.To);

                    // Rate limiting - small delay between emails
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send bulk email to {To}", message.To);
                    results.Add(new EmailSendResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message,
                        Exception = ex
                    });
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return results;
    }

    private async Task<EmailSendResult> SendMimeMessageAsync(MimeMessage mimeMessage, string recipient)
    {
        await _semaphore.WaitAsync();
        try
        {
            await EnsureConnectedAsync();
            await _client!.SendAsync(mimeMessage);

            _logger.LogInformation("Email sent successfully to {To}", recipient);
            return new EmailSendResult
            {
                Success = true,
                MessageId = mimeMessage.MessageId,
                SentAt = DateTime.UtcNow
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task EnsureConnectedAsync()
    {
        if (_client == null || !_client.IsConnected)
        {
            _client?.Dispose();
            _client = new SmtpClient();
            _client.Timeout = _config.Timeout;

            await _client.ConnectAsync(_config.Host, _config.Port, _config.EnableSsl);
            await _client.AuthenticateAsync(_config.Username, _config.Password);
        }
    }

    private MimeMessage ConvertToMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(MailboxAddress.Parse(_config.Username));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));

        if (!string.IsNullOrEmpty(message.Cc))
        {
            foreach (var cc in message.Cc.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                mimeMessage.Cc.Add(MailboxAddress.Parse(cc.Trim()));
            }
        }

        if (!string.IsNullOrEmpty(message.Bcc))
        {
            foreach (var bcc in message.Bcc.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc.Trim()));
            }
        }

        mimeMessage.Subject = message.Subject;

        if (message.IsHtml)
        {
            mimeMessage.Body = new TextPart("html") { Text = message.Body };
        }
        else
        {
            mimeMessage.Body = new TextPart("plain") { Text = message.Body };
        }

        // Set priority
        mimeMessage.Priority = message.Priority switch
        {
            EmailPriority.High => MessagePriority.Urgent,
            EmailPriority.Low => MessagePriority.NonUrgent,
            _ => MessagePriority.Normal
        };

        // Add custom headers
        foreach (var header in message.Headers)
        {
            mimeMessage.Headers.Add(header.Key, header.Value);
        }

        if (!string.IsNullOrEmpty(message.InReplyTo))
        {
            mimeMessage.InReplyTo = message.InReplyTo;
        }

        if (!string.IsNullOrEmpty(message.References))
        {
            mimeMessage.References.Add(message.References);
        }

        return mimeMessage;
    }

    public void Dispose()
    {
        _client?.Dispose();
        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}