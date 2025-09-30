using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DigitalMe.Tests.Integration;

/// <summary>
/// Collection fixture for sharing WebApplicationFactory across multiple test classes.
/// Significantly improves performance by creating the factory only once per test run.
/// </summary>
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces for the test collection.
}