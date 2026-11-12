namespace Api.Domain;

public static class ValidationKeys
{
    /// <summary>Converts <c>Address.City</c> to <c>address.city</c>.</summary>
    public static string ToCamelCase(string path) =>
        string.Join('.', path.Split('.').Select(s => s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s[1..]));
}
