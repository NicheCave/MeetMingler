using System.Text.Json;

namespace MeetMingler.BLL.Caching;

public static class CacheKeys
{
    public const string Events = "events";

    public static string Event(Guid eventId) => $"event:{eventId}";

    public static string User(Guid userId) => $"user:{userId}";

    public static string Of(string prefix, object parameters) => $"{prefix}:{JsonSerializer.Serialize(parameters)}";
}
