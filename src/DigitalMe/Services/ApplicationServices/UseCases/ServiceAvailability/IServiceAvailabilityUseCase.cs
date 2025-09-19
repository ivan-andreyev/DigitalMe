using DigitalMe.Common;
using DigitalMe.Services.ApplicationServices.Commands;
using DigitalMe.Services.ApplicationServices.Queries;

namespace DigitalMe.Services.ApplicationServices.UseCases.ServiceAvailability;

/// <summary>
/// Use case for service availability checking operations.
/// Implements Clean Architecture Application Services layer patterns.
/// </summary>
public interface IServiceAvailabilityUseCase : IApplicationService
{
    /// <summary>
    /// Executes service availability check workflow.
    /// </summary>
    Task<Result<ServiceAvailabilityResult>> ExecuteAsync(ServiceAvailabilityQuery query);
}

/// <summary>
/// Query for service availability checking.
/// </summary>
public record ServiceAvailabilityQuery(string serviceName);

/// <summary>
/// Result of service availability operations.
/// </summary>
public record ServiceAvailabilityResult(
    bool success,
    string serviceName,
    bool serviceAvailable,
    Dictionary<string, object>? additionalData = null,
    string? message = null,
    string? errorMessage = null);