using Api.Application.Abstractions;
using FluentValidation;
using FluentValidation.Results;

namespace Api.Application.Behaviors;

/// <summary>Runs every FluentValidation validator of the command before the inner handler.</summary>
public class ValidationCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(command, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        var handled = await inner.HandleAsync(command, cancellationToken);

        return handled;
    }
}
