using BenchmarkDotNet.Running;
using DigitalMe.Tests.Performance;

/// <summary>
/// Benchmark runner for Dynamic API Configuration System performance tests.
/// Run with: dotnet run -c Release --project tests/DigitalMe.Tests.Performance
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ApiConfigurationBenchmarks>();
        Console.WriteLine($"\n✅ Benchmarks complete! See {summary.ResultsDirectoryPath} for detailed results.");
    }
}