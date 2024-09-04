namespace DigitalMe.Web.Models;

public class ThreadPoolSettings
{
    public int MinWorkerThreads { get; set; } = 4;
    public int MinCompletionPortThreads { get; set; } = 4;
    public int MaxWorkerThreads { get; set; } = 100;
    public int MaxCompletionPortThreads { get; set; } = 100;
}