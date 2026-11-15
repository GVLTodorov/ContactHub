using System.Text.RegularExpressions;

using ContactHub.Domain.Exceptions;

namespace ContactHub.Domain.ValueObjects;

public sealed partial record Iban
{
    /// <summary>Total IBAN length per country, from the SWIFT IBAN registry.</summary>
    public static readonly IReadOnlyDictionary<string, int> CountryLengths = new Dictionary<string, int>
    {
        ["AD"] = 24,
        ["AE"] = 23,
        ["AL"] = 28,
        ["AT"] = 20,
        ["AZ"] = 28,
        ["BA"] = 20,
        ["BE"] = 16,
        ["BG"] = 22,
        ["BH"] = 22,
        ["BI"] = 27,
        ["BR"] = 29,
        ["BY"] = 28,
        ["CH"] = 21,
        ["CR"] = 22,
        ["CY"] = 28,
        ["CZ"] = 24,
        ["DE"] = 22,
        ["DJ"] = 27,
        ["DK"] = 18,
        ["DO"] = 28,
        ["EE"] = 20,
        ["EG"] = 29,
        ["ES"] = 24,
        ["FI"] = 18,
        ["FK"] = 18,
        ["FO"] = 18,
        ["FR"] = 27,
        ["GB"] = 22,
        ["GE"] = 22,
        ["GI"] = 23,
        ["GL"] = 18,
        ["GR"] = 27,
        ["GT"] = 28,
        ["HN"] = 28,
        ["HR"] = 21,
        ["HU"] = 28,
        ["IE"] = 22,
        ["IL"] = 23,
        ["IQ"] = 23,
        ["IS"] = 26,
        ["IT"] = 27,
        ["JO"] = 30,
        ["KW"] = 30,
        ["KZ"] = 20,
        ["LB"] = 28,
        ["LC"] = 32,
        ["LI"] = 21,
        ["LT"] = 20,
        ["LU"] = 20,
        ["LV"] = 21,
        ["LY"] = 25,
        ["MC"] = 27,
        ["MD"] = 24,
        ["ME"] = 22,
        ["MK"] = 19,
        ["MN"] = 20,
        ["MR"] = 27,
        ["MT"] = 31,
        ["MU"] = 30,
        ["NI"] = 28,
        ["NL"] = 18,
        ["NO"] = 15,
        ["OM"] = 23,
        ["PK"] = 24,
        ["PL"] = 28,
        ["PS"] = 29,
        ["PT"] = 25,
        ["QA"] = 29,
        ["RO"] = 24,
        ["RS"] = 22,
        ["RU"] = 33,
        ["SA"] = 24,
        ["SC"] = 31,
        ["SD"] = 18,
        ["SE"] = 24,
        ["SI"] = 19,
        ["SK"] = 24,
        ["SM"] = 27,
        ["SO"] = 23,
        ["ST"] = 25,
        ["SV"] = 28,
        ["TL"] = 23,
        ["TN"] = 24,
        ["TR"] = 26,
        ["UA"] = 29,
        ["VA"] = 22,
        ["VG"] = 24,
        ["XK"] = 20,
        ["YE"] = 30,
    };

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

    /// <summary>
    /// Rebuilds an IBAN that was valid when it was stored, without checking today's rules, so a
    /// stricter rule never makes existing rows unreadable. For persistence only.
    /// </summary>
    public static Iban FromTrusted(string value) => new(value);

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
        if (!FormatRegex().IsMatch(candidate) || !HasCountryLength(candidate) || !HasValidChecksum(candidate))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }

    private static bool HasCountryLength(string iban) =>
        CountryLengths.TryGetValue(iban[..2], out var length) && iban.Length == length;

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
