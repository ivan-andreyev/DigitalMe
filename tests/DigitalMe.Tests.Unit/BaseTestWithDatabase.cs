namespace DigitalMe.Tests.Unit;

using DigitalMe.Data;
using DigitalMe.Tests.Unit.Fixtures;
using Microsoft.EntityFrameworkCore;

public abstract class BaseTestWithDatabase : IDisposable
{
    private bool _disposed = false;
    protected DigitalMeDbContext Context { get; private set; }

    protected BaseTestWithDatabase()
    {
        var options = new DbContextOptionsBuilder<DigitalMeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .EnableSensitiveDataLogging()
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
        if (_disposed) return;
        
        this.Context.Database.EnsureDeleted();
        this.Context.Database.EnsureCreated();
        this.SeedIvanPersonality();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                this.Context?.Database?.EnsureDeleted();
            }
            catch
            {
                // Ignore disposal errors
            }
            finally
            {
                this.Context?.Dispose();
                _disposed = true;
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
