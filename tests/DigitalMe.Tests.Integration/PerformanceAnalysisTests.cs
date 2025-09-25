using DigitalMe.Data.Entities;
using DigitalMe.Extensions;
using DigitalMe.Services;
using DigitalMe.Services.ApplicationServices.ResponseStyling;
using DigitalMe.Services.FileProcessing;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Xunit;

namespace DigitalMe.Tests.Integration;

/// <summary>
/// Performance analysis tests for Ivan-Level Agent system optimization
/// </summary>
public class PerformanceAnalysisTests : IClassFixture<ServiceIntegrationTestFixture>
{
    private readonly ServiceIntegrationTestFixture _fixture;

    public PerformanceAnalysisTests(ServiceIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ResponseStyling_Performance_ShouldProcessTextEfficiently()
    {
        // Arrange
        var responseStylingService = _fixture.ServiceProvider.GetRequiredService<IIvanResponseStylingService>();
        var stopwatch = new Stopwatch();
        var testText = """
            This is a comprehensive technical analysis that requires detailed consideration of multiple factors.
            We need to approach this programming challenge systematically and ensure optimal performance.
            The implementation should follow SOLID principles and maintain high code quality standards.
            Performance optimization is critical for scalability and user experience in production environments.
            """;

        var context = new SituationalContext
        {
            ContextType = ContextType.Technical,
            UrgencyLevel = 0.7
        };

        var iterations = 100;
        var totalTime = TimeSpan.Zero;

        // Act - Performance measurement
        for (int i = 0; i < iterations; i++)
        {
            stopwatch.Restart();
            var result = await responseStylingService.StyleResponseAsync(testText, context);
            stopwatch.Stop();

            totalTime = totalTime.Add(stopwatch.Elapsed);

            // Verify result quality on first iteration
            if (i == 0)
            {
                Assert.NotNull(result);
                Assert.NotEmpty(result);
                Assert.Contains("C#/.NET", result);
            }
        }

        var averageTime = totalTime.TotalMilliseconds / iterations;

        // Assert - Performance requirements
        Assert.True(averageTime < 50, $"Average response styling time should be <50ms, but was {averageTime:F2}ms");
        Assert.True(totalTime.TotalMilliseconds < 2000, $"Total time for {iterations} iterations should be <2s, but was {totalTime.TotalMilliseconds:F0}ms");
    }

    [Fact]
    public async Task IvanPersonality_Performance_ShouldGeneratePromptsQuickly()
    {
        // Arrange
        var ivanService = _fixture.ServiceProvider.GetRequiredService<IPersonalityService>();
        var stopwatch = new Stopwatch();
        var iterations = 50;

        // Act & Assert - Performance measurement
        stopwatch.Start();

        for (int i = 0; i < iterations; i++)
        {
            var personality = await ivanService.GetPersonalityAsync();
            var systemPrompt = ivanService.GenerateSystemPrompt(personality.Value);
            var enhancedPrompt = await ivanService.GenerateEnhancedSystemPromptAsync();

            // Verify quality
            Assert.NotNull(personality);
            Assert.NotEmpty(systemPrompt.Value);
            Assert.NotEmpty(enhancedPrompt.Value);
        }

        stopwatch.Stop();
        var averageTime = stopwatch.ElapsedMilliseconds / (double)iterations;

        // Assert - Performance requirements
        Assert.True(averageTime < 100, $"Average personality generation time should be <100ms, but was {averageTime:F2}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 3000, $"Total time for {iterations} iterations should be <3s, but was {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task FileProcessing_Performance_ShouldHandleMultipleOperationsConcurrently()
    {
        // Arrange
        var fileService = _fixture.ServiceProvider.GetRequiredService<IFileProcessingService>();
        var stopwatch = new Stopwatch();
        var concurrentOperations = 10;
        var testContent = "Performance test content for concurrent file operations. " +
                         "This text is used to validate that file processing can handle multiple operations efficiently.";

        // Act - Concurrent operations test
        stopwatch.Start();

        var tasks = new List<Task>();

        for (int i = 0; i < concurrentOperations; i++)
        {
            var taskIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                var tempFile = Path.GetTempFileName() + ".txt";
                try
                {
                    await File.WriteAllTextAsync(tempFile, $"{testContent} Task {taskIndex}");
                    var extractedText = await fileService.ExtractTextAsync(tempFile);
                    Assert.NotEmpty(extractedText);
                    Assert.Contains($"Task {taskIndex}", extractedText);
                }
                finally
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var averageTime = stopwatch.ElapsedMilliseconds / (double)concurrentOperations;

        // Assert - Performance requirements (relaxed for CI environment)
        Assert.True(averageTime < 300, $"Average concurrent file processing time should be <300ms, but was {averageTime:F2}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 2500, $"Total concurrent processing time should be <2.5s, but was {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void DependencyInjection_Performance_ShouldResolveServicesQuickly()
    {
        // Arrange
        var stopwatch = new Stopwatch();
        var iterations = 1000;
        var serviceTypes = new[]
        {
            typeof(IPersonalityService),
            typeof(IIvanResponseStylingService),
            typeof(IFileProcessingService),
            typeof(IProfileDataParser)
        };

        // Act - Service resolution performance test
        stopwatch.Start();

        for (int i = 0; i < iterations; i++)
        {
            foreach (var serviceType in serviceTypes)
            {
                var service = _fixture.ServiceProvider.GetRequiredService(serviceType);
                Assert.NotNull(service);
            }
        }

        stopwatch.Stop();
        var averageTime = stopwatch.ElapsedMilliseconds / (double)(iterations * serviceTypes.Length);

        // Assert - Performance requirements
        Assert.True(averageTime < 1, $"Average service resolution time should be <1ms, but was {averageTime:F3}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Total service resolution time should be <500ms, but was {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task MemoryUsage_Analysis_ShouldStayWithinReasonableBounds()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var responseStylingService = _fixture.ServiceProvider.GetRequiredService<IIvanResponseStylingService>();
        var ivanService = _fixture.ServiceProvider.GetRequiredService<IPersonalityService>();

        var testText = "Memory usage test for Ivan-Level Agent system performance analysis and optimization.";
        var context = new SituationalContext { ContextType = ContextType.Technical, UrgencyLevel = 0.5 };

        // Act - Memory intensive operations
        var tasks = new List<Task>();

        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var personality = await ivanService.GetPersonalityAsync();
                var styledText = await responseStylingService.StyleResponseAsync(testText, context);
                var vocabularyPrefs = await responseStylingService.GetVocabularyPreferencesAsync(context);

                // Verify operations completed successfully
                Assert.NotNull(personality);
                Assert.NotNull(styledText);
                Assert.NotNull(vocabularyPrefs);
            }));
        }

        await Task.WhenAll(tasks);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreaseMB = memoryIncrease / (1024.0 * 1024.0);

        // Assert - Memory usage requirements
        Assert.True(memoryIncreaseMB < 50, $"Memory increase should be <50MB, but was {memoryIncreaseMB:F2}MB");
    }

    [Fact]
    public async Task EndToEnd_Performance_ShouldHandleComplexWorkflowEfficiently()
    {
        // Arrange - Complex Ivan-Level workflow simulation
        var ivanService = _fixture.ServiceProvider.GetRequiredService<IPersonalityService>();
        var responseStylingService = _fixture.ServiceProvider.GetRequiredService<IIvanResponseStylingService>();
        var fileService = _fixture.ServiceProvider.GetRequiredService<IFileProcessingService>();

        var stopwatch = new Stopwatch();
        var iterations = 10;

        // Act - End-to-end workflow performance test
        stopwatch.Start();

        for (int i = 0; i < iterations; i++)
        {
            // Step 1: Get Ivan personality
            var personality = await ivanService.GetPersonalityAsync();

            // Step 2: Generate enhanced system prompt
            var enhancedPrompt = await ivanService.GenerateEnhancedSystemPromptAsync();

            // Step 3: Apply response styling for different contexts
            var contexts = new[]
            {
                new SituationalContext { ContextType = ContextType.Technical, UrgencyLevel = 0.8 },
                new SituationalContext { ContextType = ContextType.Professional, UrgencyLevel = 0.6 },
                new SituationalContext { ContextType = ContextType.Personal, UrgencyLevel = 0.4 }
            };

            var styledResponses = new List<string>();
            foreach (var context in contexts)
            {
                var styled = await responseStylingService.StyleResponseAsync(
                    $"Complex workflow test iteration {i} for context {context.ContextType}", context);
                styledResponses.Add(styled);
            }

            // Step 4: File processing simulation
            var tempFile = Path.GetTempFileName() + ".txt";
            try
            {
                var content = $"Iteration {i}:\n{string.Join("\n", styledResponses)}";
                await File.WriteAllTextAsync(tempFile, content);
                var extractedText = await fileService.ExtractTextAsync(tempFile);

                // Verify workflow completed successfully
                Assert.NotNull(personality);
                Assert.NotEmpty(enhancedPrompt.Value);
                Assert.Equal(3, styledResponses.Count);
                Assert.NotEmpty(extractedText);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        stopwatch.Stop();
        var averageTime = stopwatch.ElapsedMilliseconds / (double)iterations;

        // Assert - End-to-end performance requirements
        Assert.True(averageTime < 1000, $"Average end-to-end workflow time should be <1s, but was {averageTime:F2}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 8000, $"Total workflow time should be <8s, but was {stopwatch.ElapsedMilliseconds}ms");
    }
}

