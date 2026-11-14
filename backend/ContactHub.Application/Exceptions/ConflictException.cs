namespace ContactHub.Application.Exceptions;

/// <summary>The request was based on an outdated version of a resource.</summary>
public class ConflictException(string message) : Exception(message)
{
    public static ConflictException ChangedByOthers(string resource, object key) =>
        new($"{resource} '{key}' was changed by someone else. Reload it to see the latest version.");
}
