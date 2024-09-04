using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using DigitalMe.MAUI.Models;

namespace DigitalMe.MAUI.Services;

public interface ISignalRChatService
{
    Task InitializeAsync();
    Task SendMessageAsync(string message, string userId, string conversationId);
    Task JoinChatAsync(string userId, string platform = "MAUI");
    Task LeaveChatAsync(string userId);
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    event EventHandler<TypingIndicatorEventArgs>? TypingIndicator;
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    HubConnectionState ConnectionState { get; }
    Task DisposeAsync();
}

public class SignalRChatService : ISignalRChatService, IAsyncDisposable
{
    private readonly MauiConfiguration _configuration;
    private readonly ILogger<SignalRChatService> _logger;
    private HubConnection? _hubConnection;

    public SignalRChatService(IOptions<MauiConfiguration> configuration, ILogger<SignalRChatService> logger)
    {
        _configuration = configuration.Value;
        _logger = logger;
    }

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<TypingIndicatorEventArgs>? TypingIndicator;
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;

    public async Task InitializeAsync()
    {
        if (!_configuration.Features.UseRealSignalR)
        {
            _logger.LogInformation("Real SignalR is disabled, using fallback");
            return;
        }

        try
        {
            var hubUrl = $"{_configuration.ApiBaseUrl}{_configuration.SignalRHub}";
            _logger.LogInformation("Initializing SignalR connection to: {HubUrl}", hubUrl);

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    // MAUI-specific configuration
                    options.HttpMessageHandlerFactory = (message) =>
                    {
                        var handler = new HttpClientHandler();
                        // For development - ignore SSL certificate errors
                        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                        return handler;
                    };
                })
                .WithAutomaticReconnect(new[] { 
                    TimeSpan.Zero, 
                    TimeSpan.FromSeconds(2), 
                    TimeSpan.FromSeconds(10), 
                    TimeSpan.FromSeconds(30) 
                })
                .Build();

            // Setup event handlers
            _hubConnection.On<MessageDto>("MessageReceived", OnMessageReceived);
            _hubConnection.On<TypingIndicatorDto>("TypingIndicator", OnTypingIndicator);
            _hubConnection.On<string>("Error", OnError);
            _hubConnection.On<object>("Connected", OnConnected);

            // Connection state change handlers
            _hubConnection.Closed += OnConnectionClosed;
            _hubConnection.Reconnected += OnReconnected;
            _hubConnection.Reconnecting += OnReconnecting;

            await _hubConnection.StartAsync();
            _logger.LogInformation("SignalR connection established successfully");

            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                NewState = ConnectionState,
                IsConnected = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SignalR connection");
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                NewState = HubConnectionState.Disconnected,
                IsConnected = false,
                Error = ex.Message
            });
        }
    }

    public async Task SendMessageAsync(string message, string userId, string conversationId)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("SignalR connection is not established");
        }

        var request = new ChatRequestDto
        {
            Message = message,
            UserId = userId,
            ConversationId = conversationId,
            Platform = "MAUI",
            Timestamp = DateTime.UtcNow
        };

        await _hubConnection.InvokeAsync("SendMessage", request);
        _logger.LogDebug("Message sent via SignalR: {MessagePreview}", 
            message.Length > 50 ? message[..50] + "..." : message);
    }

    public async Task JoinChatAsync(string userId, string platform = "MAUI")
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("SignalR connection is not established");
        }

        await _hubConnection.InvokeAsync("JoinChat", userId, platform);
        _logger.LogDebug("Joined chat for user: {UserId} from {Platform}", userId, platform);
    }

    public async Task LeaveChatAsync(string userId)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            return; // Already disconnected
        }

        await _hubConnection.InvokeAsync("LeaveChat", userId);
        _logger.LogDebug("Left chat for user: {UserId}", userId);
    }

    private void OnMessageReceived(MessageDto message)
    {
        _logger.LogDebug("Message received via SignalR: {MessageId}", message.Id);
        MessageReceived?.Invoke(this, new MessageReceivedEventArgs { Message = message });
    }

    private void OnTypingIndicator(TypingIndicatorDto indicator)
    {
        _logger.LogDebug("Typing indicator: {IsTyping} from {User}", indicator.IsTyping, indicator.User);
        TypingIndicator?.Invoke(this, new TypingIndicatorEventArgs { Indicator = indicator });
    }

    private void OnError(string error)
    {
        _logger.LogError("SignalR error received: {Error}", error);
    }

    private void OnConnected(object connectionInfo)
    {
        _logger.LogInformation("SignalR connection confirmed: {ConnectionInfo}", connectionInfo);
    }

    private Task OnConnectionClosed(Exception? exception)
    {
        _logger.LogWarning("SignalR connection closed. Exception: {Exception}", exception?.Message);
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
        {
            NewState = HubConnectionState.Disconnected,
            IsConnected = false,
            Error = exception?.Message
        });
        
        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
        {
            NewState = HubConnectionState.Connected,
            IsConnected = true
        });
        
        return Task.CompletedTask;
    }

    private Task OnReconnecting(Exception? exception)
    {
        _logger.LogWarning("SignalR attempting to reconnect. Exception: {Exception}", exception?.Message);
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
        {
            NewState = HubConnectionState.Reconnecting,
            IsConnected = false
        });
        
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _logger.LogInformation("SignalR connection disposed");
        }
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return new ValueTask(DisposeAsync());
    }
}

// Event argument classes
public class MessageReceivedEventArgs : EventArgs
{
    public required MessageDto Message { get; set; }
}

public class TypingIndicatorEventArgs : EventArgs
{
    public required TypingIndicatorDto Indicator { get; set; }
}

public class ConnectionStateChangedEventArgs : EventArgs
{
    public HubConnectionState NewState { get; set; }
    public bool IsConnected { get; set; }
    public string? Error { get; set; }
}

// DTOs for SignalR communication
public class ChatRequestDto
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Platform { get; set; } = "MAUI";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class TypingIndicatorDto
{
    public bool IsTyping { get; set; }
    public string User { get; set; } = string.Empty;
    public string? Message { get; set; }
}