using DigitalMe.Services.ApplicationServices.UseCases.Email;
using DigitalMe.Services.Email.Models;
using Microsoft.AspNetCore.Mvc;

namespace DigitalMe.Controllers;

/// <summary>
/// Email operations controller for Ivan-Level email capabilities
/// Provides REST API endpoints for email sending and receiving functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailUseCase _emailUseCase;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IEmailUseCase emailUseCase, ILogger<EmailController> logger)
    {
        _emailUseCase = emailUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Send a simple email
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult<EmailSendResult>> SendEmail([FromBody] SendEmailRequest request)
    {
        try
        {
            var result = await _emailUseCase.SendEmailAsync(request.To, request.Subject, request.Body, request.IsHtml);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", request.To);
            return StatusCode(500, new { Error = "Internal server error while sending email" });
        }
    }

    /// <summary>
    /// Send email with attachments
    /// </summary>
    [HttpPost("send-with-attachments")]
    public async Task<ActionResult<EmailSendResult>> SendEmailWithAttachments([FromBody] SendEmailWithAttachmentsRequest request)
    {
        try
        {
            var result = await _emailUseCase.SendEmailWithAttachmentsAsync(
                request.To,
                request.Subject,
                request.Body,
                request.AttachmentPaths,
                request.IsHtml);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachments to {To}", request.To);
            return StatusCode(500, new { Error = "Internal server error while sending email" });
        }
    }

    /// <summary>
    /// Get recent unread emails
    /// </summary>
    [HttpGet("unread")]
    public async Task<ActionResult<IEnumerable<EmailMessage>>> GetUnreadEmails([FromQuery] int maxCount = 10)
    {
        try
        {
            var emails = await _emailUseCase.GetRecentUnreadEmailsAsync(maxCount);
            return Ok(emails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread emails");
            return StatusCode(500, new { Error = "Internal server error while retrieving emails" });
        }
    }

    /// <summary>
    /// Search emails by subject
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<EmailMessage>>> SearchEmails([FromQuery] string keyword, [FromQuery] int maxResults = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { Error = "Keyword is required" });
            }

            var emails = await _emailUseCase.SearchEmailsBySubjectAsync(keyword, maxResults);
            return Ok(emails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search emails with keyword {Keyword}", keyword);
            return StatusCode(500, new { Error = "Internal server error while searching emails" });
        }
    }

    /// <summary>
    /// Mark emails as read
    /// </summary>
    [HttpPost("mark-read")]
    public async Task<ActionResult<int>> MarkEmailsAsRead([FromBody] MarkEmailsRequest request)
    {
        try
        {
            var markedCount = await _emailUseCase.MarkEmailsAsReadAsync(request.MessageIds);
            return Ok(new { MarkedCount = markedCount, TotalCount = request.MessageIds.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark emails as read");
            return StatusCode(500, new { Error = "Internal server error while marking emails as read" });
        }
    }

    /// <summary>
    /// Get email summary
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<EmailSummary>> GetEmailSummary()
    {
        try
        {
            var summary = await _emailUseCase.GetEmailSummaryAsync();
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get email summary");
            return StatusCode(500, new { Error = "Internal server error while getting email summary" });
        }
    }

    /// <summary>
    /// Test email service connectivity
    /// </summary>
    [HttpGet("test")]
    public async Task<ActionResult<EmailServiceStatus>> TestEmailService()
    {
        try
        {
            var status = await _emailUseCase.TestEmailServiceAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test email service");
            return StatusCode(500, new { Error = "Internal server error while testing email service" });
        }
    }
}

/// <summary>
/// Request model for sending simple email
/// </summary>
public class SendEmailRequest
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
}

/// <summary>
/// Request model for sending email with attachments
/// </summary>
public class SendEmailWithAttachmentsRequest
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public IEnumerable<string> AttachmentPaths { get; set; } = Enumerable.Empty<string>();
}

/// <summary>
/// Request model for marking emails as read
/// </summary>
public class MarkEmailsRequest
{
    public IEnumerable<string> MessageIds { get; set; } = Enumerable.Empty<string>();
}