namespace ContactHub.Application.Abstractions;

/// <summary>
/// Runs one kind of <see cref="ICommand{TResult}"/>: applies its change and returns the outcome.
/// </summary>
/// <typeparam name="TCommand">The command this handler runs.</typeparam>
/// <typeparam name="TResult">What the handler returns once the command has run.</typeparam>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    /// <summary>
    /// Runs the command.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    /// <returns>The outcome of the command.</returns>
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
