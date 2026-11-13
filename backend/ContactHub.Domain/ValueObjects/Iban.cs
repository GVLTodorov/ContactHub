using System.Text.RegularExpressions;

using ContactHub.Domain.Exceptions;

namespace ContactHub.Domain.ValueObjects;

public sealed partial record Iban
{
    private Iban(string value) => Value = value;

    public string Value { get; }

    public static bool IsValid(string? raw) => TryNormalize(raw, out _);

    public static Iban Create(string? raw)
    {
        if (!TryNormalize(raw, out var normalized))
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["iban"] = ["IBAN is not valid."],
            });
        }

        return new Iban(normalized);
    }

    /// <summary>Groups of four characters, e.g. <c>DE89 3704 0044 0532 0130 00</c>.</summary>
    public string ToFormatted() =>
        string.Join(' ', Enumerable.Range(0, (Value.Length + 3) / 4).Select(i => Value.Substring(i * 4, Math.Min(4, Value.Length - i * 4))));

    private static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var candidate = raw.Replace(" ", string.Empty).ToUpperInvariant();
        if (!FormatRegex().IsMatch(candidate) || !HasValidChecksum(candidate))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }

    private static bool HasValidChecksum(string iban)
    {
        var rearranged = iban[4..] + iban[..4];
        var remainder = 0;
        foreach (var ch in rearranged)
        {
            var digits = char.IsDigit(ch) ? ch.ToString() : (ch - 'A' + 10).ToString();
            foreach (var d in digits)
            {
                remainder = (remainder * 10 + (d - '0')) % 97;
            }
        }

        return remainder == 1;
    }

    [GeneratedRegex("^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$")]
    private static partial Regex FormatRegex();
}
