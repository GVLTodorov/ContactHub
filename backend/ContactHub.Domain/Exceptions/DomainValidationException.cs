using FluentValidation.Results;

namespace ContactHub.Domain.Exceptions;

public class DomainValidationException(IDictionary<string, string[]> errors)
    : Exception("One or more domain validation errors occurred.")
{
    /// <summary>Property path (camelCase, nested as <c>address.city</c>) to messages.</summary>
    public IDictionary<string, string[]> Errors { get; } = errors;

    public static DomainValidationException From(ValidationResult result, string? prefix = null) =>
        new(result.Errors
            .GroupBy(e => ValidationKeys.ToCamelCase(prefix is null ? e.PropertyName : $"{prefix}.{e.PropertyName}"))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()));
}
