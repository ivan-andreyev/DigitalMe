namespace DigitalMe.Services.ApplicationServices.Queries;

/// <summary>
/// Base interface for query handlers in CQRS pattern.
/// </summary>
/// <typeparam name="TQuery">Type of query to handle</typeparam>
/// <typeparam name="TResult">Type of result returned by the query</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query);
}