namespace ContactHub.Application.Abstractions;

/// <summary>
/// Marks a read-only request, such as listing contacts or fetching one by id.
/// It is handled by exactly one <see cref="IQueryHandler{TQuery, TResult}"/> and changes no state.
/// </summary>
/// <typeparam name="TResult">The data the query returns.</typeparam>
public interface IQuery<TResult>;
