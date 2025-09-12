namespace DigitalMe.Services.ApplicationServices.Commands;

/// <summary>
/// Base interface for commands in CQRS pattern.
/// Commands represent operations that change system state.
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Command with a return value.
/// </summary>
/// <typeparam name="TResult">Type of the result returned by the command</typeparam>
public interface ICommand<TResult> : ICommand
{
}