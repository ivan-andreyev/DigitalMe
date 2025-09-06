namespace DigitalMe.Common.Exceptions;

public abstract class DigitalMeException : Exception
{
    public string ErrorCode { get; }
    public object? ErrorData { get; }

    protected DigitalMeException(string errorCode, string message, object? errorData = null) 
        : base(message)
    {
        ErrorCode = errorCode;
        ErrorData = errorData;
    }

    protected DigitalMeException(string errorCode, string message, Exception innerException, object? errorData = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ErrorData = errorData;
    }
}

public class AgentBehaviorException : DigitalMeException
{
    public AgentBehaviorException(string message, object? errorData = null)
        : base("AGENT_BEHAVIOR_ERROR", message, errorData) { }

    public AgentBehaviorException(string message, Exception innerException, object? errorData = null)
        : base("AGENT_BEHAVIOR_ERROR", message, innerException, errorData) { }
}

public class MCPConnectionException : DigitalMeException
{
    public MCPConnectionException(string message, object? errorData = null)
        : base("MCP_CONNECTION_ERROR", message, errorData) { }

    public MCPConnectionException(string message, Exception innerException, object? errorData = null)
        : base("MCP_CONNECTION_ERROR", message, innerException, errorData) { }
}

public class PersonalityServiceException : DigitalMeException
{
    public PersonalityServiceException(string message, object? errorData = null)
        : base("PERSONALITY_SERVICE_ERROR", message, errorData) { }

    public PersonalityServiceException(string message, Exception innerException, object? errorData = null)
        : base("PERSONALITY_SERVICE_ERROR", message, innerException, errorData) { }
}

public class MessageProcessingException : DigitalMeException
{
    public MessageProcessingException(string message, object? errorData = null)
        : base("MESSAGE_PROCESSING_ERROR", message, errorData) { }

    public MessageProcessingException(string message, Exception innerException, object? errorData = null)
        : base("MESSAGE_PROCESSING_ERROR", message, innerException, errorData) { }
}

public class ExternalServiceException : DigitalMeException
{
    public ExternalServiceException(string message, object? errorData = null)
        : base("EXTERNAL_SERVICE_ERROR", message, errorData) { }

    public ExternalServiceException(string message, Exception innerException, object? errorData = null)
        : base("EXTERNAL_SERVICE_ERROR", message, innerException, errorData) { }
}