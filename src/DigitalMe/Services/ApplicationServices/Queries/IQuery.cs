namespace DigitalMe.Services.ApplicationServices.Queries;

/// <summary>
/// Base interface for queries in CQRS pattern.
/// Queries represent operations that read system state without changing it.
/// </summary>
/// <typeparam name="TResult">Type of the result returned by the query</typeparam>
public interface IQuery<TResult>
{
}