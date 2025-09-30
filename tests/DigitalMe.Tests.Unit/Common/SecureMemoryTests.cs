using Xunit;
using FluentAssertions;
using DigitalMe.Common;
using System.Runtime.InteropServices;

namespace DigitalMe.Tests.Unit.Common;

/// <summary>
/// TDD Test Suite for SecureMemory
/// RED PHASE: Tests should initially fail
/// Tests cover memory pinning, automatic zeroing, and proper disposal
/// </summary>
public class SecureMemoryTests
{
    #region Basic Functionality Tests

    [Fact]
    public void SecureMemory_Should_Store_Data()
    {
        // Arrange
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        using var secureMemory = new SecureMemory(testData);

        // Assert
        secureMemory.Data.Should().NotBeNull();
        secureMemory.Data.Should().BeEquivalentTo(testData);
    }

    [Fact]
    public void SecureMemory_Should_Accept_Empty_Array()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act
        using var secureMemory = new SecureMemory(emptyData);

        // Assert
        secureMemory.Data.Should().NotBeNull();
        secureMemory.Data.Should().BeEmpty();
    }

    [Fact]
    public void SecureMemory_Should_Reject_Null_Data()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SecureMemory(null!));
    }

    #endregion

    #region Memory Zeroing Tests

    [Fact]
    public void Dispose_Should_Zero_Out_Memory()
    {
        // Arrange
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        byte[] dataReference;

        using (var secureMemory = new SecureMemory(testData))
        {
            dataReference = secureMemory.Data;
            dataReference.Should().BeEquivalentTo(testData, "data should be accessible before disposal");
        }

        // Assert - after disposal, memory should be zeroed
        dataReference.Should().OnlyContain(b => b == 0, "memory should be zeroed after disposal");
    }

    [Fact]
    public void Multiple_Dispose_Calls_Should_Be_Safe()
    {
        // Arrange
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var secureMemory = new SecureMemory(testData);

        // Act - call Dispose multiple times
        secureMemory.Dispose();
        secureMemory.Dispose();
        secureMemory.Dispose();

        // Assert - should not throw
        // (test passes if no exception is thrown)
    }

    #endregion

    #region Memory Pinning Tests

    [Fact]
    public void SecureMemory_Should_Pin_Memory()
    {
        // Arrange
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        using var secureMemory = new SecureMemory(testData);

        // Assert - memory should be pinned (can't directly test, but verify no crash)
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Data should still be accessible after GC
        secureMemory.Data.Should().BeEquivalentTo(testData);
    }

    [Fact]
    public void SecureMemory_Should_Unpin_Memory_On_Dispose()
    {
        // Arrange
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var secureMemory = new SecureMemory(testData);

        // Act
        secureMemory.Dispose();

        // Assert - memory should be unpinned (can't directly verify, but check no crash)
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Should not crash after disposal and GC
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SecureMemory_Should_Handle_Large_Arrays()
    {
        // Arrange
        var largeData = new byte[1024 * 1024]; // 1 MB
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }

        // Act
        using var secureMemory = new SecureMemory(largeData);

        // Assert
        secureMemory.Data.Length.Should().Be(1024 * 1024);
    }

    [Fact]
    public void Accessing_Data_After_Dispose_Should_Throw_ObjectDisposedException()
    {
        // Arrange
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var secureMemory = new SecureMemory(testData);

        // Act
        secureMemory.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => _ = secureMemory.Data);
    }

    [Fact]
    public void Data_Reference_Before_Dispose_Should_Be_Zeroed_After_Dispose()
    {
        // Arrange
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var secureMemory = new SecureMemory(testData);
        var dataReference = secureMemory.Data;

        // Act
        secureMemory.Dispose();

        // Assert - the original array reference should be zeroed
        dataReference.Should().OnlyContain(b => b == 0);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void SecureMemory_Creation_Should_Be_Fast()
    {
        // Arrange
        var testData = new byte[1024];
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        using var secureMemory = new SecureMemory(testData);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10,
            "SecureMemory creation should be fast (< 10ms)");
    }

    [Fact]
    public void SecureMemory_Disposal_Should_Be_Fast()
    {
        // Arrange
        var testData = new byte[1024];
        var secureMemory = new SecureMemory(testData);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        secureMemory.Dispose();
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10,
            "SecureMemory disposal should be fast (< 10ms)");
    }

    #endregion
}