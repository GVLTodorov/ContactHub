using System.Text.RegularExpressions;

using ContactHub.Domain.Exceptions;

namespace ContactHub.Domain.ValueObjects;

public sealed partial record PhoneNumber
{
    public const int MinDigits = 7;
    public const int MaxDigits = 15;

    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static bool IsValid(string? raw) => TryNormalize(raw, out _);

    public static PhoneNumber Create(string? raw)
    {
        if (!TryNormalize(raw, out var normalized))
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["phoneNumber"] = [$"Phone number must have an optional leading '+' followed by {MinDigits} to {MaxDigits} digits."],
            });
        }

        return new PhoneNumber(normalized);
    }

    /// <summary>
    /// Rebuilds a phone number that was valid when it was stored, without checking today's rules,
    /// so a stricter rule never makes existing rows unreadable. For persistence only.
    /// </summary>
    public static PhoneNumber FromTrusted(string value) => new(value);

    private static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var candidate = StripRegex().Replace(raw, string.Empty);
        var digits = candidate.StartsWith('+') ? candidate[1..] : candidate;
        if (digits.Length is < MinDigits or > MaxDigits || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }

    [GeneratedRegex(@"[ \-.()]")]
    private static partial Regex StripRegex();
}
