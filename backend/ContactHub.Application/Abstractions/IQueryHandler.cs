namespace ContactHub.Application.Abstractions;

/// <summary>
/// Answers one kind of <see cref="IQuery{TResult}"/> by reading data, without changing it.
/// </summary>
/// <typeparam name="TQuery">The query this handler answers.</typeparam>
/// <typeparam name="TResult">The data the query returns.</typeparam>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Answers the query.
    /// </summary>
    /// <param name="query">The query to answer.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    /// <returns>The requested data.</returns>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
