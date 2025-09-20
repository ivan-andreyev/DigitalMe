using System.ComponentModel;

namespace DigitalMe.Tests.E2E;

public static class E2ETestConfig
{
    public static string Environment => System.Environment.GetEnvironmentVariable("TEST_ENV") ?? "local";

    public static string ApiBaseUrl => Environment switch
    {
        "production" => "https://digitalme-api-llig7ks2ca-uc.a.run.app",
        "staging" => "https://digitalme-api-staging-llig7ks2ca-uc.a.run.app", // Future
        "local" => "http://localhost:5001",
        _ => throw new InvalidEnumArgumentException($"Unknown environment: {Environment}")
    };

    public static string WebBaseUrl => Environment switch
    {
        "production" => "https://digitalme-web-llig7ks2ca-uc.a.run.app",
        "staging" => "https://digitalme-web-staging-llig7ks2ca-uc.a.run.app", // Future
        "local" => "http://localhost:8081",
        _ => throw new InvalidEnumArgumentException($"Unknown environment: {Environment}")
    };

    public static TimeSpan HttpTimeout => Environment switch
    {
        "production" => TimeSpan.FromSeconds(30), // Cloud Run cold start
        "staging" => TimeSpan.FromSeconds(20),
        "local" => TimeSpan.FromSeconds(10),
        _ => TimeSpan.FromSeconds(15)
    };

    public static int MaxRetries => Environment switch
    {
        "production" => 3,
        "staging" => 2,
        "local" => 1,
        _ => 2
    };

    public static bool SkipExternalDependencies => Environment == "local";
}