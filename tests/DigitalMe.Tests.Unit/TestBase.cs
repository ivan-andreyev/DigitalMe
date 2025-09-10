using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DigitalMe.Tests.Unit;

public abstract class TestBase
{
    protected MockRepository MockRepository { get; }

    protected TestBase()
    {
        MockRepository = new MockRepository(MockBehavior.Strict);
    }

    protected void VerifyAll()
    {
        MockRepository.VerifyAll();
    }

    protected DbContextOptions<T> CreateInMemoryDbOptions<T>(string? databaseName = null) where T : DbContext
    {
        return new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
    }
}
