using System.Text.RegularExpressions;

using ContactHub.Domain.Exceptions;

namespace ContactHub.Domain.ValueObjects;

public sealed partial record PhoneNumber
{
    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static bool IsValid(string? raw) => TryNormalize(raw, out _);

    public static PhoneNumber Create(string? raw)
    {
        if (!TryNormalize(raw, out var normalized))
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["phoneNumber"] = ["Phone number must have an optional leading '+' followed by 7 to 15 digits."],
            });
        }

        return new PhoneNumber(normalized);
    }

    private static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var candidate = StripRegex().Replace(raw, string.Empty);
        if (!NormalizedRegex().IsMatch(candidate))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }

    [GeneratedRegex(@"[ \-.()]")]
    private static partial Regex StripRegex();

    [GeneratedRegex(@"^\+?[0-9]{7,15}$")]
    private static partial Regex NormalizedRegex();
}
