using DigitalMe.Common;
using DigitalMe.Data;
using DigitalMe.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalMe.Repositories;

/// <summary>
/// Repository implementation for managing API configuration entities.
/// Provides efficient data access with proper error handling and async patterns.
/// </summary>
public class ApiConfigurationRepository : IApiConfigurationRepository
{
    private readonly DigitalMeDbContext _context;

    /// <summary>
    /// Initializes a new instance of the ApiConfigurationRepository.
    /// </summary>
    /// <param name="context">The database context for data operations.</param>
    public ApiConfigurationRepository(DigitalMeDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<ApiConfiguration?> GetByIdAsync(Guid id)
    {
        return await _context.ApiConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApiConfiguration?> GetByUserAndProviderAsync(string userId, string provider)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));
        ValidationHelper.ValidateProvider(provider, nameof(provider));

        return await _context.ApiConfigurations
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Provider == provider && c.IsActive)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<ApiConfiguration>> GetAllByUserAsync(string userId)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));

        return await _context.ApiConfigurations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Provider)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<ApiConfiguration>> GetActiveConfigurationsAsync(string userId)
    {
        ValidationHelper.ValidateUserId(userId, nameof(userId));

        return await _context.ApiConfigurations
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderBy(c => c.Provider)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApiConfiguration> CreateAsync(ApiConfiguration configuration)
    {
        ValidationHelper.ValidateNotNull(configuration, nameof(configuration));

        await _context.ApiConfigurations.AddAsync(configuration).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        return configuration;
    }

    /// <inheritdoc />
    public async Task<ApiConfiguration> UpdateAsync(ApiConfiguration configuration)
    {
        ValidationHelper.ValidateNotNull(configuration, nameof(configuration));

        _context.ApiConfigurations.Update(configuration);

        try
        {
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Re-throw to let the caller handle concurrency issues
            throw;
        }

        return configuration;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id)
    {
        var configuration = await _context.ApiConfigurations.FindAsync(id).ConfigureAwait(false);

        if (configuration == null)
            return false;

        _context.ApiConfigurations.Remove(configuration);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }
}