namespace ContactHub.Application.Abstractions;

/// <summary>
/// Marks a request that changes state, such as creating, updating or deleting a contact.
/// It is handled by exactly one <see cref="ICommandHandler{TCommand, TResult}"/>.
/// </summary>
/// <typeparam name="TResult">What the handler returns once the command has run.</typeparam>
public interface ICommand<TResult>;
