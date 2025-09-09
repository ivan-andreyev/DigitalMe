using Microsoft.EntityFrameworkCore;
using DigitalMe.Data;
using DigitalMe.Tests.Unit.Fixtures;

namespace DigitalMe.Tests.Unit;

public abstract class BaseTestWithDatabase : IDisposable
{
    protected DigitalMeDbContext Context { get; private set; }

    protected BaseTestWithDatabase()
    {
        var options = new DbContextOptionsBuilder<DigitalMeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Context = new DigitalMeDbContext(options);
        Context.Database.EnsureCreated();
        SeedIvanPersonality();
    }

    protected void SeedIvanPersonality()
    {
        var ivan = PersonalityTestFixtures.CreateCompleteIvanProfile();
        ivan.Name = "Ivan";

        Context.PersonalityProfiles.Add(ivan);
        Context.SaveChanges();
    }

    protected void CleanupDatabase()
    {
        Context.Database.EnsureDeleted();
        Context.Database.EnsureCreated();
        SeedIvanPersonality();
    }

    public void Dispose() => Context?.Dispose();
}
