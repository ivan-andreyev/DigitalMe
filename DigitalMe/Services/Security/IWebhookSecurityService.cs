namespace DigitalMe.Services.Security;

public interface IWebhookSecurityService
{
    bool ValidateWebhookSignature(string signature, string payload, string secret);
    Task<bool> IsRequestAuthorizedAsync(string authHeader);
}

public class WebhookSecurityService : IWebhookSecurityService
{
    public bool ValidateWebhookSignature(string signature, string payload, string secret)
    {
        throw new NotImplementedException("WebhookSecurityService implementation pending");
    }

    public Task<bool> IsRequestAuthorizedAsync(string authHeader)
    {
        throw new NotImplementedException("WebhookSecurityService implementation pending");
    }
}