using DigitalMe.Models;

namespace DigitalMe.Services.AgentBehavior;

public interface IAgentBehaviorEngine
{
    Task<AgentResponse> ProcessMessageAsync(string message, PersonalityContext context);
    Task<MoodAnalysis> AnalyzeMoodAsync(string message, PersonalityProfile personality);
    Task<bool> ShouldTriggerToolAsync(string toolName, string message, PersonalityContext context);
    Task<Dictionary<string, object>> GenerateContextMetadataAsync(PersonalityContext context);
}

public class AgentResponse
{
    public string Content { get; set; } = string.Empty;
    public MoodAnalysis Mood { get; set; } = new();
    public List<string> TriggeredTools { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public double ConfidenceScore { get; set; } = 1.0;
}

public class MoodAnalysis
{
    public string PrimaryMood { get; set; } = "neutral";
    public double Intensity { get; set; } = 0.5;
    public Dictionary<string, double> MoodScores { get; set; } = new();
    public string Context { get; set; } = string.Empty;
}
