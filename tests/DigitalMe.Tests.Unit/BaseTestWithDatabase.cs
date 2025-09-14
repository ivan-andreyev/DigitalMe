using DigitalMe.Data;
using DigitalMe.Tests.Unit.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace DigitalMe.Tests.Unit;

public abstract class BaseTestWithDatabase : IDisposable
{
    protected DigitalMeDbContext Context { get; private set; }

    protected BaseTestWithDatabase()
    {
        var options = new DbContextOptionsBuilder<DigitalMeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        this.Context = new DigitalMeDbContext(options);
        this.Context.Database.EnsureCreated();
        this.SeedIvanPersonality();
    }

    protected void SeedIvanPersonality()
    {
        var ivan = PersonalityTestFixtures.CreateCompleteIvanProfile();
        ivan.Name = "Ivan";

        this.Context.PersonalityProfiles.Add(ivan);
        this.Context.SaveChanges();
    }

    protected void CleanupDatabase()
    {
        this.Context.Database.EnsureDeleted();
        this.Context.Database.EnsureCreated();
        this.SeedIvanPersonality();
    }

    public void Dispose() => this.Context?.Dispose();
}
